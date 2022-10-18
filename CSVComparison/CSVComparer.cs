using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
        private string[] _headerColumns = null;

        public CSVComparer(ComparisonDefinition comparisonDefinition)
        {
            _comparisonDefinition = comparisonDefinition;
        }

        public ComparisonResult CompareFiles(string referenceFile, string candidateFile)
        {
            ResetState();

            var referenceLoaderTask = Task.Run(() => LoadFile(referenceFile, _referenceQueue));
            var candidateLoaderTask = Task.Run(() => LoadFile(candidateFile, _candidateQueue));
            var compareTask = Task.Run(() => CompareCsvs());

            Task.WaitAll(referenceLoaderTask, candidateLoaderTask, compareTask);

            if (!_earlyTerminate)
            {
                foreach (var extracandidate in _candidateOrphans)
                {
                    AddOrphan(extracandidate, BreakType.RowInCandidateNotInReference);
                }

                foreach (var extraReference in _referenceOrphans)
                {
                    AddOrphan(extraReference, BreakType.RowInReferenceNotInCandidate);             
                }
            }

            return new ComparisonResult(_breaks) { 
                ReferenceSource = referenceFile, 
                CandidateSource = candidateFile, 
                NumberOfReferenceRows = _numberOfReferenceRows,
                NumberOfCandidateRows = _numberOfCandidateRows,
                Date = DateTime.Now
            };
        }

        private void AddOrphan(KeyValuePair<string, CsvRow> orphan, BreakType breakType)
        {
            bool excludeBreak = false;

            foreach (var exclusion in _comparisonDefinition.OrphanExclusions)
            {
                if (Regex.IsMatch(orphan.Key, exclusion))
                {
                    excludeBreak = true;
                    break;
                }
            }

            if (!excludeBreak)
            {
                _breaks.Add(new BreakDetail()
                {                 
                    BreakType = breakType,
                    BreakKey = orphan.Key,
                    ReferenceRow = breakType == BreakType.RowInReferenceNotInCandidate ? orphan.Value.RowIndex : -1,
                    CandidateRow = breakType == BreakType.RowInCandidateNotInReference ? orphan.Value.RowIndex : -1,
                    BreakDescription = $"Key missing: {orphan.Key}"
                });
            }
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
            _headerColumns = null;
        }

        /// <summary>
        /// Task used for loading csv data from each input file
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
                        if (_comparisonDefinition.Delimiter.Length == 1 && line.IndexOf("\"") > -1)
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
                                _excludedColumns ??= excludedColumns;
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

        /// <summary>
        /// Task used to compare the data from the two input files
        /// </summary>
        void CompareCsvs()
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

                    if (_referenceQueue.Count == 0 && _candidateQueue.Count == 0 && _runningLoaderThreads == 0)
                    {
                        complete = true;
                    }
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
                        var foundReferenceOrphan = GetOrAddOrphan(candidateRow, _referenceOrphans, _candidateOrphans);
                        if (foundReferenceOrphan != null)
                        { 
                            CompareRow(candidateRow.Key, foundReferenceOrphan, candidateRow);
                        }

                        // See if the reference row has a matching row in candidate orphans

                        var foundCandidateOrphan = GetOrAddOrphan(referenceRow, _candidateOrphans, _referenceOrphans);
                        if (foundCandidateOrphan != null)
                        {
                            CompareRow(referenceRow.Key, referenceRow, foundCandidateOrphan);
                        }
                    }
                }
                else if (candidateRow != null)
                {
                    var foundReferenceOrphan = GetOrAddOrphan(candidateRow, _referenceOrphans, _candidateOrphans);
                    if (foundReferenceOrphan != null)
                    {             
                        CompareRow(candidateRow.Key, foundReferenceOrphan, candidateRow);
                    }
                }
                else if (referenceRow != null)
                {
                    var foundCandidateOrphan = GetOrAddOrphan(referenceRow, _candidateOrphans, _referenceOrphans);
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
            if (_headerCheck)
            {
                _headerCheck = false;
                _headerColumns = referenceRow.Columns;

                // Early return for mismatching header
                if (!CompareValues(key, referenceRow, candidateRow))
                {
                    lock (_lockObj)
                    {
                        _earlyTerminate = true;
                    }                
                }
            }
            else
            {
                CompareValues(key, referenceRow, candidateRow);
            }
        }

        /// <summary>
        /// Gets the orphan corresponding to the supplied CSV row. If that doesn't exist add CSV row to its own orphans dictionary
        /// </summary>
        /// <param name="row">CSV row we want to compare to</param>
        /// <param name="existingOrphans">Existing orphans to check against</param>
        /// <param name="orphansToAdd">Orphans for the current CSV row</param>
        /// <returns>Existing Orphan row if it exists or null</returns>
        /// <exception cref="ComparisonException">Duplicate key in orphan dictionary</exception>
        private CsvRow GetOrAddOrphan(CsvRow row, Dictionary<string, CsvRow> existingOrphans, Dictionary<string, CsvRow> orphansToAdd)
        {
            CsvRow existingOrphan = null;
            if (existingOrphans.ContainsKey(row.Key))
            {
                existingOrphan = existingOrphans[row.Key];
                existingOrphans.Remove(row.Key);
            }
            else
            {
                if (orphansToAdd.ContainsKey(row.Key))
                {
                    throw new ComparisonException($"Orphan key: {row.Key} already exists. This usually means the key columns do not define unique rows.");
                }

                orphansToAdd.Add(row.Key, row);
            }

            return existingOrphan;
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
                var columnName = _headerColumns[referenceIndex];

                if (_comparisonDefinition.ToleranceType != ToleranceType.Exact)
                {
                    success &= CompareWithTolerance(key, columnName, referenceValue, candidateValue, referenceRow.RowIndex, candidateRow.RowIndex);
                }
                else if (referenceValue != candidateValue)
                {
                    success = false;
                    AddBreak(BreakType.ValueMismatch, key, referenceRow.RowIndex, candidateRow.RowIndex, columnName, referenceValue, candidateValue);
                }
            }

            return success;
        }

        private bool CompareWithTolerance(string key, string columnName, string referenceValue, string candidateValue, int referenceRowIndex, int candidateRowIndex)
        {
            var success = true;

            if (double.TryParse(referenceValue.Trim('\"'), out double referenceDouble) && double.TryParse(candidateValue.Trim('\"'), out double candidateDouble))
            {
                switch (_comparisonDefinition.ToleranceType)
                {
                    case ToleranceType.Absolute:
                        if (Math.Abs(referenceDouble - candidateDouble) > _comparisonDefinition.ToleranceValue)
                        {
                            success = false;
                            AddBreak(BreakType.ValueMismatch, key, referenceRowIndex, candidateRowIndex, columnName, referenceValue, candidateValue);
                        }
                        break;
                    case ToleranceType.Relative:
                        var relativeDifference = (referenceDouble - candidateDouble) / referenceDouble;
                        if (Math.Abs(relativeDifference) > _comparisonDefinition.ToleranceValue)
                        {
                            success = false;
                            AddBreak(BreakType.ValueMismatch, key, referenceRowIndex, candidateRowIndex, columnName, referenceValue, candidateValue);
                        }
                        break;
                    case ToleranceType.Exact:
                    default:
                        if (referenceDouble != candidateDouble)
                        {
                            success = false;
                            AddBreak(BreakType.ValueMismatch, key, referenceRowIndex, candidateRowIndex, columnName, referenceValue, candidateValue);
                        }
                        
                        break;
                }
            }        
            else if (referenceValue != candidateValue)
            {
                // Default to string comparison
                success = false;
                AddBreak(BreakType.ValueMismatch, key, referenceRowIndex, candidateRowIndex, columnName, referenceValue, candidateValue);
            }

            return success;
        }

        void AddBreak(BreakType breakType,
            string breakKey,
            int referenceRowIndex,
            int candidateRowIndex,
            string columnName,
            string referenceValue,
            string candidateValue)
        {
            foreach (var exclusion in _comparisonDefinition.KeyExclusions)
            {
                if (Regex.IsMatch(breakKey, exclusion))
                {
                    return;
                }
            }

            _breaks.Add(new BreakDetail(breakType, breakKey, referenceRowIndex, candidateRowIndex, columnName, referenceValue, candidateValue));
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
        /// <param name="line">The full CSV line</param>
        /// <returns>List of each CSV Column</returns>
        public List<string> SplitStringWithQuotes(string line)
        {
            var startingQuoteIndex = line.IndexOf("\"");
            var columnValues = new List<string>();

            int quoteSearchIndex = 0;
            int endQuoteIndex;
            int currentIndex;
            while ((currentIndex = line.IndexOf(_comparisonDefinition.Delimiter, quoteSearchIndex)) > 0)
            {
                int startIndex = quoteSearchIndex;
                
                if (startingQuoteIndex > -1 && startingQuoteIndex >= quoteSearchIndex && startingQuoteIndex < currentIndex)
                {
                    // Get the end quote
                    endQuoteIndex = GetEndQuoteIndex(line, startingQuoteIndex);
                    if (endQuoteIndex == -1 || endQuoteIndex == line.Length - 1)
                    {
                        currentIndex = line.Length;
                    }
                    else
                    {
                        currentIndex = endQuoteIndex + 1;
                        startingQuoteIndex = line.IndexOf("\"", currentIndex + 1);
                    }
                }

                columnValues.Add(line.Substring(startIndex, currentIndex - startIndex));
                if (currentIndex < line.Length)
                {
                    quoteSearchIndex = currentIndex + 1;
                }
                else
                {
                    quoteSearchIndex = currentIndex;
                }
            }

            if (quoteSearchIndex < line.Length)
            {
                columnValues.Add(line.Substring(quoteSearchIndex, line.Length - quoteSearchIndex));
            }

            // If the last character is a delimiter we will use the convention that this indicates there is one more column 
            if (line.EndsWith(_comparisonDefinition.Delimiter))
            {
                columnValues.Add("");
            }

            return columnValues;
        }

        /// <summary>
        /// Get the location of the end matching quote
        /// </summary>
        /// <param name="line">The full CSV line</param>
        /// <param name="startingQuoteIndex">Index of the opening quite</param>
        /// <returns>The index of the end quote matching the opening quote</returns>
        /// <remarks>As per CSV RFC-4180 pairs of quotes are ignored, ""
        /// For example A,B,"A ""Test"" Value
        /// Will return "A ""Test"" Value" as a single field</remarks>
        private static int GetEndQuoteIndex(string line, int startingQuoteIndex)
        {
            bool terminated = false;
            int queryIndex = startingQuoteIndex;
            while (!terminated)
            {
                int nextQuoteIndex = line.IndexOf("\"", queryIndex + 1);

                if (nextQuoteIndex + 1 == line.Length || (nextQuoteIndex + 1 < line.Length && line[nextQuoteIndex + 1] != '\"'))
                {
                    return nextQuoteIndex;
                }
                else if (line[nextQuoteIndex + 1] == '\"')
                {
                    //This is a double quote
                    queryIndex = nextQuoteIndex + 1;
                }
                else if (nextQuoteIndex == -1)
                {
                    terminated = true;
                }
                else
                {
                    throw new Exception($"Unable to determine quotes for {line}");
                }
            }

            return -1;
        }
    }
}
