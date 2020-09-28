using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CSVComparison
{
    /// <summary>
    /// Main class for comparing two CSV files
    /// There are three threads. Two for loading and one for comparing
    /// </summary>
    public class CSVComparer
    {
        private ManualResetEvent _readyToStartComparisonEvent = new ManualResetEvent(false);
        private readonly object _lockObj = new object();
        private Queue<CsvRow> _referenceQueue = new Queue<CsvRow>();
        private Queue<CsvRow> _candidateQueue = new Queue<CsvRow>();
        private int _runningLoaderThreads = 2;
        private ComparisonDefinition _comparisonDefinition;
        private Dictionary<string, CsvRow> _referenceOrphans = new Dictionary<string, CsvRow>();
        private Dictionary<string, CsvRow> _candidateOrphans = new Dictionary<string, CsvRow>();
        private List<BreakDetail> _breaks = new List<BreakDetail>();
        private bool _headerCheck = true;
        private bool _earlyTerminate = false;
        private long _numberOfReferenceRows = 0;
        private long _numberOfCandidateRows = 0;
        private HashSet<int> _excludedColumns = null;

        public CSVComparer(ComparisonDefinition comparisonDefinition)
        {
            _comparisonDefinition = comparisonDefinition;
        }

        public ComparisonResult CompareFiles(string referenceFile, string candidateFile)
        {
            ResetState();

            var referenceLoaderTask = Task.Run(() => LoadFile(referenceFile, _referenceQueue));
            var candidateLoaderTask = Task.Run(() => LoadFile(candidateFile, _candidateQueue));
            var compareTask = Task.Run(() => Compare());

            Task.WaitAll(referenceLoaderTask, candidateLoaderTask, compareTask);

            if (!_earlyTerminate)
            {
                foreach (var extracandidate in _candidateOrphans)
                {
                    _breaks.Add(new BreakDetail() { BreakType = BreakType.RowInCandidateNotInReference, BreakKey = extracandidate.Key, CandidateRow = extracandidate.Value.RowIndex });
                }

                foreach (var extraReference in _referenceOrphans)
                {
                    _breaks.Add(new BreakDetail() { BreakType = BreakType.RowInReferenceNotInCandidate, BreakKey = extraReference.Key, ReferenceRow = extraReference.Value.RowIndex});
                }
            }

            return new ComparisonResult(_breaks) { 
                ReferenceSource = referenceFile, 
                CandidateSource = candidateFile, 
                NumberOfReferenceRows = _numberOfReferenceRows,
                NumberOfCandidateRows = _numberOfCandidateRows };
        }

        private void ResetState()
        {
            _readyToStartComparisonEvent = new ManualResetEvent(false);    
            _referenceQueue.Clear();
            _candidateQueue.Clear();
            _referenceOrphans.Clear();
            _candidateOrphans.Clear();
            _breaks.Clear();
            _headerCheck = true;
            _earlyTerminate = false;
            _runningLoaderThreads = 2;
            _numberOfReferenceRows = 0;
            _numberOfCandidateRows = 0;
            _excludedColumns = null;
        }

        /// <summary>
        /// The thread function for loading data from each input file
        /// </summary>
        /// <param name="file">Full path to csv file</param>
        /// <param name="queue">Target data queue</param>
        void LoadFile(string file, Queue<CsvRow> queue)
        {
            try
            {
                using (var fileStream = File.OpenRead(file))
                using (var streamReader = new StreamReader(fileStream))
                {
                    string line;
                    int rowIndex = 0;
                    bool dataRow = false;
                    int expectedColumnCount = 0;
                    List<int> keyIndexes = new List<int>();

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        string[] columns;
                        if (_comparisonDefinition.Delimiter.Length == 1 || line.IndexOf("\"") > -1)
                        {
                            // If the delimiter is in quotes we don't want to split on it
                            // However complex delimiters do not support this
                            columns = SplitStringWithQuotes(line).ToArray();
                        }
                        else
                        {
                            columns = line.Split(_comparisonDefinition.Delimiter);
                        }

                        if (rowIndex == _comparisonDefinition.HeaderRowIndex)
                        {
                            keyIndexes.AddRange(GetKeyIndexes(columns));
                            expectedColumnCount = columns.Length;
                            dataRow = true;

                            // Both loader threads can set excluded columns, but we only want to update once
                            // If the columns are different we will early terminate the comparison
                            var excludedColumns = GetExcludedColumns(columns);
                            lock(_lockObj)
                            {
                                if (_excludedColumns == null)
                                {
                                    _excludedColumns = excludedColumns;
                                }
                            }
                        }

                        if (dataRow)
                        {
                            if (columns.Length == expectedColumnCount || !_comparisonDefinition.IgnoreInvalidRows)
                            {
                                string key = "";
                                foreach (int index in keyIndexes)
                                {
                                    key += columns[index] + ":";
                                }

                                key = key.Trim(':');

                                lock (_lockObj)
                                {
                                    queue.Enqueue(new CsvRow() { Key = key, Columns = columns, RowIndex = rowIndex });
                                }
                            }

                            _readyToStartComparisonEvent.Set();
                        }

                        rowIndex++;
                    }

                    if (rowIndex == 0)
                    {
                        // There were no rows in this file. 
                        _readyToStartComparisonEvent.Set();
                    }
                }
            }
            catch (Exception ex)
            {
                var message = $"Problem loading {file} : {ex.Message}";
                Console.WriteLine(message);
                lock (_lockObj)
                {
                    _breaks.Add(new BreakDetail() { BreakType = BreakType.ProcessFailure, BreakDescription = message });
                    _earlyTerminate = true;
                }

                _readyToStartComparisonEvent.Set();
            }

            Interlocked.Decrement(ref _runningLoaderThreads);
        }

        void Compare()
        {
            // We want to wait until at least one loader has started producing data
            _readyToStartComparisonEvent.WaitOne();
            bool complete = false;

            while (!complete)
            {
                if (_earlyTerminate)
                {
                    complete = true;
                    continue;
                }

                CsvRow referenceRow = null;
                CsvRow candidateRow = null;
                lock (_lockObj)
                {
                    if (_referenceQueue.Count > 0)
                    {
                        referenceRow = _referenceQueue.Dequeue();
                        _numberOfReferenceRows++;
                    }
                    if (_candidateQueue.Count > 0)
                    {
                        candidateRow = _candidateQueue.Dequeue();
                        _numberOfCandidateRows++;
                    }
                }
               
                if (referenceRow == null && candidateRow == null && _runningLoaderThreads == 0)
                {
                    complete = true;
                    continue;
                }

                if (referenceRow != null && candidateRow != null)
                {
                    // Both rows have the same key
                    if (referenceRow.Key == candidateRow.Key)
                    {                              
                        CompareRow(referenceRow.Key, referenceRow, candidateRow);
                    }
                    else
                    {
                        // See if the candidate row has a matching row in reference orphans
                        var foundReferenceOrphan = CheckReferenceOrphan(candidateRow);
                        if (foundReferenceOrphan != null)
                        { 
                            CompareRow(candidateRow.Key, foundReferenceOrphan, candidateRow);
                        }

                        // See if the reference row has a matching row in candidate orphans
                        var foundCandidateOrphan = CheckCandidateOrphan(referenceRow);
                        if (foundCandidateOrphan != null)
                        {
                            CompareRow(referenceRow.Key, referenceRow, foundCandidateOrphan);
                        }
                    }
                }
                else if (candidateRow != null)
                {
                    var foundReferenceOrphan = CheckReferenceOrphan(candidateRow);
                    if (foundReferenceOrphan != null)
                    {             
                        CompareRow(candidateRow.Key, foundReferenceOrphan, candidateRow);
                    }
                }
                else if (referenceRow != null)
                {
                    var foundCandidateOrphan = CheckCandidateOrphan(referenceRow);
                    if (foundCandidateOrphan != null)
                    {
                        CompareRow(referenceRow.Key, referenceRow, foundCandidateOrphan);
                    }
                }  
            }     
        }

        /// <summary>
        /// Check reference and candidate rows that have the same key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="lhsColumns"></param>
        /// <param name="rhsColumns"></param>
        void CompareRow(string key, CsvRow referenceRow, CsvRow candidateRow)
        {
            bool success = CompareValues(key, referenceRow, candidateRow);
            if (_headerCheck)
            {
                _headerCheck = false;
                // Early return for mismatching header
                if (!success)
                {
                    lock (_lockObj)
                    {
                        _earlyTerminate = true;
                    }                
                }
            }
        }

        private CsvRow CheckReferenceOrphan(CsvRow candidateRow)
        {
            CsvRow csvRow = null;

            if (_referenceOrphans.ContainsKey(candidateRow.Key))
            {
                csvRow = _referenceOrphans[candidateRow.Key];
                _referenceOrphans.Remove(candidateRow.Key);
       
            }
            else
            {
                if (_candidateOrphans.ContainsKey(candidateRow.Key))
                {
                    throw new ComparisonException($"Candidate orphan {candidateRow.Key} already exists. This usually means the key columns do not define unique rows.");
                }

                _candidateOrphans.Add(candidateRow.Key, candidateRow);
            }

            return csvRow;
        }

        private CsvRow CheckCandidateOrphan(CsvRow referenceRow)
        {
            CsvRow csvRow = null;
            if (_candidateOrphans.ContainsKey(referenceRow.Key))
            {             
                csvRow = _candidateOrphans[referenceRow.Key];          
                _candidateOrphans.Remove(referenceRow.Key);
             
            }
            else
            {
                if (_referenceOrphans.ContainsKey(referenceRow.Key))
                {
                    throw new ComparisonException($"Reference orphan {referenceRow.Key} already exists. This usually means the key columns do not define unique rows.");
                }

                _referenceOrphans.Add(referenceRow.Key, referenceRow);
            }

            return csvRow;
        }

        /// <summary>
        /// Compare the actual values of reference and candidare rows
        /// </summary>
        /// <param name="key"></param>
        /// <param name="referenceRow"></param>
        /// <param name="candidateRow"></param>
        /// <returns>True for successful comparison</returns>
        /// <remarks>We assume the columns will be in the same order</remarks>   
        bool CompareValues(string key, CsvRow referenceRow, CsvRow candidateRow)
        {
            var referenceColumns = referenceRow.Columns;
            var candidateColumns = candidateRow.Columns;

            if (referenceColumns.Length != candidateColumns.Length)
            { 
                _breaks.Add(new BreakDetail() { BreakType = BreakType.ColumnsDifferent, BreakDescription = $"Reference has {referenceColumns.Length} columns, Candidate has {candidateColumns.Length} columns" });
                return false;
            }

            bool success = true;

            for (int referenceIndex = 0; referenceIndex < referenceColumns.Length; referenceIndex++)
            {

                // Don't lock - _excludedColumns is only updated by one of the loader threads
                if (_excludedColumns.Contains(referenceIndex))
                {
                    continue;
                }
                
                var referenceValue = referenceColumns[referenceIndex];
                var candidateValue = candidateColumns[referenceIndex];

                if (_comparisonDefinition.ToleranceType != ToleranceType.Exact)
                {
                    success &= CompareWithTolerance(key, referenceValue, candidateValue, referenceRow.RowIndex, candidateRow.RowIndex);
                }              
                else if (referenceValue != candidateValue)
                {
                    success = false;
                    _breaks.Add(new BreakDetail(BreakType.ValueMismatch, key, referenceRow.RowIndex, candidateRow.RowIndex, referenceValue, candidateValue));
                }
            }

            return success;
        }

        private bool CompareWithTolerance(string key, string referenceValue, string candidateValue, int referenceRowIndex, int candidateRowIndex)
        {
            var success = true;
            double referenceDouble, candidateDouble;
            if (double.TryParse(referenceValue, out referenceDouble) && double.TryParse(candidateValue, out candidateDouble))
            {
                if (_comparisonDefinition.ToleranceType == ToleranceType.Absolute)
                {
                    if (Math.Abs(referenceDouble - candidateDouble) > _comparisonDefinition.ToleranceValue)
                    {
                        success = false;
                        _breaks.Add(new BreakDetail(BreakType.ValueMismatch, key, referenceRowIndex, candidateRowIndex, referenceValue, candidateValue));
                    }
                }
                else if (_comparisonDefinition.ToleranceType == ToleranceType.Relative)
                {
                    double relativeDifference = (referenceDouble - candidateDouble) / referenceDouble;
                    if (Math.Abs(relativeDifference) > _comparisonDefinition.ToleranceValue)
                    {
                        success = false;
                        _breaks.Add(new BreakDetail(BreakType.ValueMismatch, key, referenceRowIndex, candidateRowIndex, referenceValue, candidateValue));
                    }
                }
            }
            else if (referenceValue != candidateValue)
            {
                success = false;
                _breaks.Add(new BreakDetail(BreakType.ValueMismatch, key, referenceRowIndex, candidateRowIndex, referenceValue, candidateValue));
            }

            return success;
        }

        List<int> GetKeyIndexes(string[] headerRow)
        {
            var keyIndexes = new List<int>();

            for (int columnIndex = 0; columnIndex < headerRow.Length; columnIndex++)
            {
                if (_comparisonDefinition.KeyColumns.Contains(headerRow[columnIndex]))
                {
                    keyIndexes.Add(columnIndex);
                }
            }

            if (_comparisonDefinition.KeyColumns.Count > 0 && keyIndexes.Count == 0)
            {
                throw new ComparisonException("No columns match the key columns defined in configuration");
            }

            return keyIndexes;
        }

        HashSet<int> GetExcludedColumns(string[] headerRow)
        {
            var excludedColumns = new HashSet<int>();

            for(int columnIndex = 0; columnIndex < headerRow.Length; columnIndex++)
            {
                if (_comparisonDefinition.ExcludedColumns != null && _comparisonDefinition.ExcludedColumns.Contains(headerRow[columnIndex]))
                {
                    excludedColumns.Add(columnIndex);
                }
            }

            return excludedColumns;
        }

        /// <summary>
        /// Split a string that can have delimiters embedded in quotes, for example: A,B,"C,D",E
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public List<string> SplitStringWithQuotes(string line)
        {
            var startingQuoteIndex = line.IndexOf("\"");
            var columnValues = new List<string>();

            int lastIndex = 0;
            int currentIndex = 0;
            while ((currentIndex = line.IndexOf(_comparisonDefinition.Delimiter, lastIndex)) > 0)
            {
                int startIndex = lastIndex;

                if (startingQuoteIndex > -1 && startingQuoteIndex >= lastIndex && startingQuoteIndex < currentIndex)
                {
                    // Get the next quote
                    int nextQuoteIndex = line.IndexOf("\"", startingQuoteIndex + 1);
                    
                    // If the next qoute is past the current delimiter index, get the next delimiter index
                    while (nextQuoteIndex > currentIndex && nextQuoteIndex < line.Length -1)
                    {
                        lastIndex = currentIndex + 1;
                        currentIndex = line.IndexOf(_comparisonDefinition.Delimiter, lastIndex);
                    }

                    if (nextQuoteIndex == line.Length - 1)
                    {
                        // The next quote is the final character in the line 
                        startIndex += 1;
                        currentIndex = line.Length - 1;
                    }
                    else
                    {
                        // Get next startingQuoteIndex;
                        startingQuoteIndex = line.IndexOf("\"", currentIndex + 1);
                    }
                }
       
                columnValues.Add(line.Substring(startIndex, currentIndex - startIndex));
                lastIndex = currentIndex + 1;            
            }

            if (lastIndex < line.Length)
            {
                columnValues.Add(line.Substring(lastIndex, line.Length - lastIndex));
            }

            // If the last character is a delimiter we will use the convention that this indicates there is one more column 
            if (line.EndsWith(_comparisonDefinition.Delimiter))
            {
                columnValues.Add("");
            }

            return columnValues;
        }
    }
}
