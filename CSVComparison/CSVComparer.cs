using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CSVComparison
{
    public class CSVComparer
    {
        private ManualResetEvent _firstRowLoadedEvent = new ManualResetEvent(false);
        private readonly object _lockObj = new object();
        private readonly Queue<CsvRow> _referenceQueue = new Queue<CsvRow>();
        private readonly Queue<CsvRow> _targetQueue = new Queue<CsvRow>();
        private int _runningLoaderThreads = 2;
        private ComparisonDefinition _comparisonDefinition;
        private Dictionary<string, string[]> _referenceOrphans = new Dictionary<string, string[]>();
        private Dictionary<string, string[]> _targetOrphans = new Dictionary<string, string[]>();
        private List<BreakDetail> _breaks = new List<BreakDetail>();
        private bool _headerCheck = true;
        private bool _earlyTerminate = false;

        public CSVComparer(ComparisonDefinition comparisonDefinition)
        {
            _comparisonDefinition = comparisonDefinition;
        }

        public ComparisonResult CompareFiles(string referenceFile, string targetFile)
        {
            var referenceLoaderTask = Task.Run(() => LoadFile(referenceFile, _referenceQueue));
            var targetLoaderTask = Task.Run(() => LoadFile(targetFile, _targetQueue));
            var compareTask = Task.Run(() => Compare());

            Task.WaitAll(referenceLoaderTask, targetLoaderTask, compareTask);

            if (!_earlyTerminate)
            {
                foreach (var extraTarget in _targetOrphans)
                {
                    _breaks.Add(new BreakDetail() { BreakType = BreakType.RowInTargetNotInReference, BreakDescription = extraTarget.Key });
                }

                foreach (var extraReference in _referenceOrphans)
                {
                    _breaks.Add(new BreakDetail() { BreakType = BreakType.RowInReferenceNotInTarget, BreakDescription = extraReference.Key });
                }
            }

            return new ComparisonResult(_breaks) { ReferenceSource = referenceFile, TargetSource = targetFile };
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

                            _firstRowLoadedEvent.Set();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Problem loading {file} : {ex.Message}");
                _firstRowLoadedEvent.Set();
            }

            Interlocked.Decrement(ref _runningLoaderThreads);
        }

        void Compare()
        {
            _firstRowLoadedEvent.WaitOne();
            bool complete = false;

            while (!complete)
            {
                CsvRow referenceRow = null;
                CsvRow targetRow = null;
                lock (_lockObj)
                {
                    if (_referenceQueue.Count > 0)
                    {
                        referenceRow = _referenceQueue.Dequeue();
                    }
                    if (_targetQueue.Count > 0)
                    {
                        targetRow = _targetQueue.Dequeue();
                    }
                }

                string[] lhsColumns = null;
                string[] rhsColumns = null;
                string key = "";
               
                if (referenceRow == null && targetRow == null && _runningLoaderThreads == 0)
                {
                    complete = true;
                    continue;
                }
                var lhs = referenceRow != null ? referenceRow.Key : "null Ref";
                var rhs = targetRow != null ? targetRow.Key : "null Target";
                Console.WriteLine($"{lhs} : {rhs}");

                if (referenceRow != null && targetRow != null)
                {
                    if (referenceRow.Key == targetRow.Key)
                    {
                        lhsColumns = referenceRow.Columns;
                        rhsColumns = targetRow.Columns;
                        key = referenceRow.Key;
                        _earlyTerminate = CompareRow(key, lhsColumns, rhsColumns);
                    }
                    else
                    {
                        if (CheckReferenceOrphan(targetRow, ref key, ref lhsColumns, ref rhsColumns))
                        {
                            _earlyTerminate = CompareRow(key, lhsColumns, rhsColumns);
                        }

                        if (CheckTargetOrphan(referenceRow, ref key, ref lhsColumns, ref rhsColumns))
                        {
                            _earlyTerminate = CompareRow(key, lhsColumns, rhsColumns);
                        }
                    }
                }
                else if (targetRow != null)
                {
                    if (CheckReferenceOrphan(targetRow, ref key, ref lhsColumns, ref rhsColumns))
                    {
                        _earlyTerminate = CompareRow(key, lhsColumns, rhsColumns);
                    }

                }
                else if (referenceRow != null)
                {
                    if (CheckTargetOrphan(referenceRow, ref key, ref lhsColumns, ref rhsColumns))
                    {
                        _earlyTerminate = CompareRow(key, lhsColumns, rhsColumns);
                    }
                }  

                if (_earlyTerminate)
                {
                    return;
                }
            }     
        }

        bool CompareRow(string key, string[] lhsColumns, string[] rhsColumns)
        {
            Console.WriteLine($"Run compare {key}");
            bool success = CompareValues(key, lhsColumns, rhsColumns);
            if (_headerCheck)
            {
                _headerCheck = false;
                // Early return for mismatching header
                if (!success)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CheckReferenceOrphan(CsvRow targetRow, ref string key, ref string[] lhsColumns, ref string[] rhsColumns)
        {
            Console.WriteLine($"Check for {targetRow.Key} in _referenceOrphans");
            bool doCompare = false;
            if (_referenceOrphans.ContainsKey(targetRow.Key))
            {
                lhsColumns = _referenceOrphans[targetRow.Key];
                rhsColumns = targetRow.Columns;
                key = targetRow.Key;
                Console.WriteLine($"remove reference orphan {targetRow.Key}");
                _referenceOrphans.Remove(targetRow.Key);
                doCompare = true;
            }
            else
            {
                Console.WriteLine($"Add target orphan {targetRow.Key}");
                _targetOrphans.Add(targetRow.Key, targetRow.Columns);
            }

            return doCompare;
        }

        private bool CheckTargetOrphan(CsvRow referenceRow, ref string key, ref string[] lhsColumns, ref string[] rhsColumns)
        {
            Console.WriteLine($"Check for {referenceRow.Key} in _targetOrphans");
            bool doCompare = false;
            if (_targetOrphans.ContainsKey(referenceRow.Key))
            {
                lhsColumns = referenceRow.Columns; 
                rhsColumns = _targetOrphans[referenceRow.Key];
                key = referenceRow.Key;
                Console.WriteLine($"remove target orphan {referenceRow.Key}");
                _targetOrphans.Remove(referenceRow.Key);
                doCompare = true;
            }
            else
            {
                Console.WriteLine($"Add reference orphan {referenceRow.Key}");
                _referenceOrphans.Add(referenceRow.Key, referenceRow.Columns);
            }

            return doCompare;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="referenceRow"></param>
        /// <param name="targetRow"></param>
        /// <returns>True for successful comparison</returns>
        /// <remarks>We assume the columns will be in the same order</remarks>   
        bool CompareValues(string key, string[] referenceRow, string[] targetRow)
        {
            if (referenceRow.Length != targetRow.Length)
            {
                _breaks.Add(new BreakDetail() { BreakType = BreakType.ColumnsDifferent, BreakDescription = $"Reference has {referenceRow.Length} columns, Target has {targetRow.Length} columns" });
                return false;
            }

            bool success = true;
            for (int referenceIndex = 0; referenceIndex < referenceRow.Length; referenceIndex++)
            {
                string referenceValue = referenceRow[referenceIndex];
                string targetValue = targetRow[referenceIndex];
                if (referenceValue != targetValue)
                {
                    success = false;
                    _breaks.Add(new BreakDetail() { BreakType = BreakType.ValueMismatch, BreakDescription = $"{key}: {referenceValue} != {targetValue}" });
                }
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
