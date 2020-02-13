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
        private readonly Queue<CsvRow> _referenceQueue = new Queue<CsvRow>();
        private readonly Queue<CsvRow> _candidateQueue = new Queue<CsvRow>();
        private int _runningLoaderThreads = 2;
        private ComparisonDefinition _comparisonDefinition;
        private Dictionary<string, string[]> _referenceOrphans = new Dictionary<string, string[]>();
        private Dictionary<string, string[]> _candidateOrphans = new Dictionary<string, string[]>();
        private List<BreakDetail> _breaks = new List<BreakDetail>();
        private bool _headerCheck = true;
        private bool _earlyTerminate = false;

        public ComparisonResult CompareFiles(string referenceFile, string candidateFile, ComparisonDefinition comparisonDefinition)
        {       
            _comparisonDefinition = comparisonDefinition;

            var referenceLoaderTask = Task.Run(() => LoadFile(referenceFile, _referenceQueue));
            var candidateLoaderTask = Task.Run(() => LoadFile(candidateFile, _candidateQueue));
            var compareTask = Task.Run(() => Compare());

            Task.WaitAll(referenceLoaderTask, candidateLoaderTask, compareTask);

            if (!_earlyTerminate)
            {
                foreach (var extracandidate in _candidateOrphans)
                {
                    _breaks.Add(new BreakDetail() { BreakType = BreakType.RowInCandidateNotInReference, BreakDescription = extracandidate.Key });
                }

                foreach (var extraReference in _referenceOrphans)
                {
                    _breaks.Add(new BreakDetail() { BreakType = BreakType.RowInReferenceNotInCandidate, BreakDescription = extraReference.Key });
                }
            }

            return new ComparisonResult(_breaks) { ReferenceSource = referenceFile, CandidateSource = candidateFile };
        }

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
                    List<int> keyIndexes = new List<int>();
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        // This doesn't manage delimiter characters in comments, i.e. A,"B,Comment",C
                        string[] columns = line.Split(_comparisonDefinition.Delimiter);

                        if (rowIndex++ == _comparisonDefinition.HeaderRowIndex)
                        {
                            keyIndexes.AddRange(GetKeyIndexes(columns));
                            dataRow = true;
                        }

                        if (dataRow)
                        {
                            string key = "";
                            foreach (int index in keyIndexes)
                            {
                                key += columns[index];
                            }

                            lock (_lockObj)
                            {
                                queue.Enqueue(new CsvRow() { Key = key, Columns = columns });
                            }

                            _readyToStartComparisonEvent.Set();
                        }
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
                    }
                    if (_candidateQueue.Count > 0)
                    {
                        candidateRow = _candidateQueue.Dequeue();
                    }
                }

                string[] lhsColumns = null;
                string[] rhsColumns = null;
                string key = "";
               
                if (referenceRow == null && candidateRow == null && _runningLoaderThreads == 0)
                {
                    complete = true;
                    continue;
                }

                var lhs = referenceRow != null ? referenceRow.Key : "null Ref";
                var rhs = candidateRow != null ? candidateRow.Key : "null candidate";
                // Console.WriteLine($"{lhs} : {rhs}");

                if (referenceRow != null && candidateRow != null)
                {
                    // Both rows have the same key
                    if (referenceRow.Key == candidateRow.Key)
                    {
                        lhsColumns = referenceRow.Columns;
                        rhsColumns = candidateRow.Columns;
                        key = referenceRow.Key;
                        CompareRow(key, lhsColumns, rhsColumns);
                    }
                    else
                    {
                        // See if the candidate row has a matching row in reference orphans
                        if (CheckReferenceOrphan(candidateRow, ref key, ref lhsColumns, ref rhsColumns))
                        {
                            CompareRow(key, lhsColumns, rhsColumns);
                        }

                        // See if the reference row has a matching row in candidate orphans
                        if (CheckcandidateOrphan(referenceRow, ref key, ref lhsColumns, ref rhsColumns))
                        {
                            CompareRow(key, lhsColumns, rhsColumns);
                        }
                    }
                }
                else if (candidateRow != null)
                {
                    if (CheckReferenceOrphan(candidateRow, ref key, ref lhsColumns, ref rhsColumns))
                    {
                        CompareRow(key, lhsColumns, rhsColumns);
                    }

                }
                else if (referenceRow != null)
                {
                    if (CheckcandidateOrphan(referenceRow, ref key, ref lhsColumns, ref rhsColumns))
                    {
                        CompareRow(key, lhsColumns, rhsColumns);
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
        void CompareRow(string key, string[] lhsColumns, string[] rhsColumns)
        {
            Console.WriteLine($"Run compare {key}");
            bool success = CompareValues(key, lhsColumns, rhsColumns);
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

        private bool CheckReferenceOrphan(CsvRow candidateRow, ref string key, ref string[] lhsColumns, ref string[] rhsColumns)
        {
            Console.WriteLine($"Check for {candidateRow.Key} in _referenceOrphans");
            bool doCompare = false;

            if (_referenceOrphans.ContainsKey(candidateRow.Key))
            {
                lhsColumns = _referenceOrphans[candidateRow.Key];
                rhsColumns = candidateRow.Columns;
                key = candidateRow.Key;
                // Console.WriteLine($"remove reference orphan {candidateRow.Key}");
                _referenceOrphans.Remove(candidateRow.Key);
                doCompare = true;
            }
            else
            {
                // Console.WriteLine($"Add candidate orphan {candidateRow.Key}");
                _candidateOrphans.Add(candidateRow.Key, candidateRow.Columns);
            }

            return doCompare;
        }

        private bool CheckcandidateOrphan(CsvRow referenceRow, ref string key, ref string[] lhsColumns, ref string[] rhsColumns)
        {
            Console.WriteLine($"Check for {referenceRow.Key} in _candidateOrphans");
            bool doCompare = false;
            if (_candidateOrphans.ContainsKey(referenceRow.Key))
            {
                lhsColumns = referenceRow.Columns; 
                rhsColumns = _candidateOrphans[referenceRow.Key];
                key = referenceRow.Key;
                // Console.WriteLine($"remove candidate orphan {referenceRow.Key}");
                _candidateOrphans.Remove(referenceRow.Key);
                doCompare = true;
            }
            else
            {
                // Console.WriteLine($"Add reference orphan {referenceRow.Key}");
                _referenceOrphans.Add(referenceRow.Key, referenceRow.Columns);
            }

            return doCompare;
        }

        /// <summary>
        /// Compare the actual values of reference and candidare rows
        /// </summary>
        /// <param name="key"></param>
        /// <param name="referenceRow"></param>
        /// <param name="candidateRow"></param>
        /// <returns>True for successful comparison</returns>
        /// <remarks>We assume the columns will be in the same order</remarks>   
        bool CompareValues(string key, string[] referenceRow, string[] candidateRow)
        {
            if (referenceRow.Length != candidateRow.Length)
            { 
                _breaks.Add(new BreakDetail() { BreakType = BreakType.ColumnsDifferent, BreakDescription = $"Reference has {referenceRow.Length} columns, Candidate has {candidateRow.Length} columns" });
                return false;
            }

            bool success = true;

            for (int referenceIndex = 0; referenceIndex < referenceRow.Length; referenceIndex++)
            {
                var referenceValue = referenceRow[referenceIndex];
                var candidateValue = candidateRow[referenceIndex];

                if (_comparisonDefinition.ToleranceType != ToleranceType.Exact)
                {
                    success &= CompareWithTolerance(key, referenceValue, candidateValue);
                }              
                else if (referenceValue != candidateValue)
                {
                    success = false;
                    _breaks.Add(new BreakDetail() { BreakType = BreakType.ValueMismatch, BreakDescription = $"{key}: {referenceValue} != {candidateValue}" });
                }
            }

            return success;
        }

        private bool CompareWithTolerance(string key, string referenceValue, string candidateValue)
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
                        _breaks.Add(new BreakDetail { BreakType = BreakType.ValueMismatch, BreakDescription = $"{key}: {referenceValue} != {candidateValue}" });
                    }
                }
                else if (_comparisonDefinition.ToleranceType == ToleranceType.Relative)
                {
                    double relativeDifference = (referenceDouble - candidateDouble) / referenceDouble;
                    if (Math.Abs(relativeDifference) > _comparisonDefinition.ToleranceValue)
                    {
                        success = false;
                        _breaks.Add(new BreakDetail { BreakType = BreakType.ValueMismatch, BreakDescription = $"{key}: {referenceValue} != {candidateValue}" });
                    }
                }
            }
            else if (referenceValue != candidateValue)
            {
                success = false;
                _breaks.Add(new BreakDetail() { BreakType = BreakType.ValueMismatch, BreakDescription = $"{key}: {referenceValue} != {candidateValue}" });
            }

            return success;
        }

        List<int> GetKeyIndexes(string[] headerRow)
        {
            List<int> keyIndexes = new List<int>();

            for (int columnIndex = 0; columnIndex < headerRow.Length; columnIndex++)
            {
                if (_comparisonDefinition.KeyColumns.Contains(headerRow[columnIndex]))
                {
                    keyIndexes.Add(columnIndex);
                }
            }

            return keyIndexes;
        }
    }
}
