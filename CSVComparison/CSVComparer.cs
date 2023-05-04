using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CSVComparison;

/// <summary>
/// Main class for comparing two CSV files
/// There are three threads. Two for loading and one for comparing
/// </summary>
public class CSVComparer
{
    private ManualResetEvent _readyToStartComparisonEvent = new(false);
    private readonly object _lockObj = new();
    private Queue<CsvRow> _leftHandSideQueue = new();
    private Queue<CsvRow> _rightHandSideQueue = new();
    private int _runningLoaderThreads = 2;
    private ComparisonDefinition _comparisonDefinition;
    private Dictionary<string, CsvRow> _leftHandSideOrphans = new();
    private Dictionary<string, CsvRow> _rightHandSideOrphans = new();
    private List<BreakDetail> _breaks = new();
    private bool _headerCheck = true;
    private bool _earlyTerminate = false;
    private long _numberOfLeftHandSideRows = 0;
    private long _numberOfRightHandSideRows = 0;
    private HashSet<int> _excludedColumns = null;
#nullable enable 
    private string[]? _headerColumns;
    private string? _keyDefinition;
#nullable disable

    public CSVComparer(ComparisonDefinition comparisonDefinition)
    {
        _comparisonDefinition = comparisonDefinition;
    }

    public ComparisonResult CompareFiles(string leftHandSideFile, string rightHandSideFile)
    {
        ResetState();

        var leftHandSideLoaderTask = Task.Run(() => LoadFile(leftHandSideFile, _leftHandSideQueue));
        var rightHandSideLoaderTask = Task.Run(() => LoadFile(rightHandSideFile, _rightHandSideQueue));
        var compareTask = Task.Run(() => CompareCsvs());

        Task.WaitAll(leftHandSideLoaderTask, rightHandSideLoaderTask, compareTask);

        if (!_earlyTerminate)
        {
            foreach (var extrarightHandSide in _rightHandSideOrphans)
            {
                AddOrphan(extrarightHandSide, BreakType.RowInRHS_NotInLHS);
            }

            foreach (var extraLeftHandSide in _leftHandSideOrphans)
            {
                AddOrphan(extraLeftHandSide, BreakType.RowInLHS_NotInRHS);             
            }
        }

        return new ComparisonResult(_breaks) { 
            KeyDefinition = _keyDefinition,
            LeftHandSideSource = leftHandSideFile, 
            RightHandSideSource = rightHandSideFile, 
            NumberOfLeftHandSideRows = _numberOfLeftHandSideRows,
            NumberOfRightHandSideRows = _numberOfRightHandSideRows,
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
                LeftHandSideRow = breakType == BreakType.RowInLHS_NotInRHS ? orphan.Value.RowIndex : -1,
                RightHandSideRow = breakType == BreakType.RowInRHS_NotInLHS ? orphan.Value.RowIndex : -1,
                BreakDescription = $"Key missing: {orphan.Key}"
            });
        }
    }

    private void ResetState()
    {
        _readyToStartComparisonEvent = new ManualResetEvent(false);    
        _leftHandSideQueue.Clear();
        _rightHandSideQueue.Clear();
        _leftHandSideOrphans.Clear();
        _rightHandSideOrphans.Clear();
        _breaks.Clear();
        _headerCheck = true;
        _earlyTerminate = false;
        _runningLoaderThreads = 2;
        _numberOfLeftHandSideRows = 0;
        _numberOfRightHandSideRows = 0;
        _excludedColumns = null;
        _headerColumns = null;
        _keyDefinition = null;
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
            var rowIndex = 0;
            var headerRow = true;
            var expectedColumnCount = 0;
            var keyIndexes = new List<int>();

            foreach (var line in File.ReadLines(file))
            {
                string[] columns;
                if (_comparisonDefinition.Delimiter.Length == 1 && line.IndexOf("\"") > -1)
                {
                    // If the delimiter is in quotes we don't want to split on it
                    // However complex delimiters do not support this
                    columns = RowHelper.SplitRowWithQuotes(line, _comparisonDefinition.Delimiter).ToArray();
                }
                else
                {
                    columns = line.Split(_comparisonDefinition.Delimiter);
                }

                if (rowIndex == _comparisonDefinition.HeaderRowIndex)
                {
                    expectedColumnCount = DecodeHeaderRow(keyIndexes, columns);
                    headerRow = false;
                }

                if (!headerRow)
                {
                    if (columns.Length == expectedColumnCount || !_comparisonDefinition.IgnoreInvalidRows)
                    {
                        var key = new StringBuilder();
                        foreach (int index in keyIndexes)
                        {
                            key.Append(columns[index]).Append(':');
                        }

                        key.Length--; // Remove trailing ':'

                        lock (_lockObj)
                        {
                            queue.Enqueue(new CsvRow() { Key = key.ToString(), Columns = columns, RowIndex = rowIndex });
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

    private int DecodeHeaderRow(List<int> keyIndexes, string[] columns)
    {
        keyIndexes.AddRange(GetKeyIndexes(columns));
        var keyCols = new List<string>();
        foreach (var index in keyIndexes)
        {
            keyCols.Add(columns[index]);
        }

        var keyDefinition = string.Join(':', keyCols);
        var expectedColumnCount = columns.Length;
      
        // Both loader threads can set excluded columns, but we only want to update once
        // If the columns are different we will early terminate the comparison
        var excludedColumns = GetExcludedColumns(columns);
        lock (_lockObj)
        {
            _excludedColumns ??= excludedColumns;
            _keyDefinition ??= keyDefinition;
        }

        return expectedColumnCount;
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

            CsvRow leftHandSideRow = null;
            CsvRow rightHandSideRow = null;
            lock (_lockObj)
            {
                if (_leftHandSideQueue.Count > 0)
                {
                    leftHandSideRow = _leftHandSideQueue.Dequeue();
                    _numberOfLeftHandSideRows++;
                }

                if (_rightHandSideQueue.Count > 0)
                {
                    rightHandSideRow = _rightHandSideQueue.Dequeue();
                    _numberOfRightHandSideRows++;
                }

                if (_leftHandSideQueue.Count == 0 && _rightHandSideQueue.Count == 0 && _runningLoaderThreads == 0)
                {
                    complete = true;
                }
            }
         
            if (leftHandSideRow != null && rightHandSideRow != null)
            {
                // Both rows have the same key
                if (leftHandSideRow.Key == rightHandSideRow.Key)
                {                              
                    CompareRow(leftHandSideRow.Key, leftHandSideRow, rightHandSideRow);
                }
                else
                {
                    // See if the rightHandSide row has a matching row in leftHandSide orphans
                    var foundLeftHandSideOrphan = GetOrAddOrphan(rightHandSideRow, _leftHandSideOrphans, _rightHandSideOrphans);
                    if (foundLeftHandSideOrphan != null)
                    { 
                        CompareRow(rightHandSideRow.Key, foundLeftHandSideOrphan, rightHandSideRow);
                    }

                    // See if the leftHandSide row has a matching row in rightHandSide orphans
                    var foundRightHandSideOrphan = GetOrAddOrphan(leftHandSideRow, _rightHandSideOrphans, _leftHandSideOrphans);
                    if (foundRightHandSideOrphan != null)
                    {
                        CompareRow(leftHandSideRow.Key, leftHandSideRow, foundRightHandSideOrphan);
                    }
                }
            }
            else if (rightHandSideRow != null)
            {
                var foundLeftHandSideOrphan = GetOrAddOrphan(rightHandSideRow, _leftHandSideOrphans, _rightHandSideOrphans);
                if (foundLeftHandSideOrphan != null)
                {             
                    CompareRow(rightHandSideRow.Key, foundLeftHandSideOrphan, rightHandSideRow);
                }
            }
            else if (leftHandSideRow != null)
            {
                var foundRightHandSideOrphan = GetOrAddOrphan(leftHandSideRow, _rightHandSideOrphans, _leftHandSideOrphans);
                if (foundRightHandSideOrphan != null)
                {
                    CompareRow(leftHandSideRow.Key, leftHandSideRow, foundRightHandSideOrphan);
                }
            }  
        }     
    }

    /// <summary>
    /// Check leftHandSide and rightHandSide rows that have the same key
    /// </summary>
    /// <param name="key"></param>
    /// <param name="lhsColumns"></param>
    /// <param name="rhsColumns"></param>
    void CompareRow(string key, CsvRow leftHandSideRow, CsvRow rightHandSideRow)
    {                
        if (_headerCheck)
        {
            _headerCheck = false;
            _headerColumns = leftHandSideRow.Columns;

            // Early return for mismatching header
            if (!CompareValues(key, leftHandSideRow, rightHandSideRow))
            {
                lock (_lockObj)
                {
                    _earlyTerminate = true;
                }                
            }
        }
        else
        {
            CompareValues(key, leftHandSideRow, rightHandSideRow);
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
    /// Compare the actual values of leftHandSide and candidare rows
    /// </summary>
    /// <param name="key"></param>
    /// <param name="leftHandSideRow"></param>
    /// <param name="rightHandSideRow"></param>
    /// <returns>True for successful comparison</returns>
    /// <remarks>We assume the columns will be in the same order</remarks>   
    bool CompareValues(string key, CsvRow leftHandSideRow, CsvRow rightHandSideRow)
    {
        var leftHandSideColumns = leftHandSideRow.Columns;
        var rightHandSideColumns = rightHandSideRow.Columns;

        if (leftHandSideColumns.Length != rightHandSideColumns.Length)
        { 
            _breaks.Add(new BreakDetail() { BreakType = BreakType.ColumnsDifferent, BreakDescription = $"LeftHandSide has {leftHandSideColumns.Length} columns, RightHandSide has {rightHandSideColumns.Length} columns" });
            return false;
        }

        bool success = true;

        for (int leftHandSideIndex = 0; leftHandSideIndex < leftHandSideColumns.Length; leftHandSideIndex++)
        {
            // Don't lock - _excludedColumns is only updated by one of the loader threads
            if (_excludedColumns.Contains(leftHandSideIndex))
            {
                continue;
            }

            var leftHandSideValue = leftHandSideColumns[leftHandSideIndex];
            var rightHandSideValue = rightHandSideColumns[leftHandSideIndex];
            var columnName = _headerColumns[leftHandSideIndex];

            success &= CompareWithTolerance(key, columnName, leftHandSideValue, rightHandSideValue, leftHandSideRow.RowIndex, rightHandSideRow.RowIndex);
        }

        return success;
    }

    private bool CompareWithTolerance(string key, string columnName, string leftHandSideValue, string rightHandSideValue, int leftHandSideRowIndex, int rightHandSideRowIndex)
    {
        var success = true;

        if (double.TryParse(leftHandSideValue.Trim('\"'), out double leftHandSideDouble) && double.TryParse(rightHandSideValue.Trim('\"'), out double rightHandSideDouble))
        {
            switch (_comparisonDefinition.ToleranceType)
            {
                case ToleranceType.Absolute:
                    if (Math.Abs(leftHandSideDouble - rightHandSideDouble) > _comparisonDefinition.ToleranceValue)
                    {
                        success = false;
                        AddBreak(BreakType.ValueMismatch, key, leftHandSideRowIndex, rightHandSideRowIndex, columnName, leftHandSideValue, rightHandSideValue);
                    }
                    break;
                case ToleranceType.Relative:
                    var relativeDifference = (leftHandSideDouble - rightHandSideDouble) / leftHandSideDouble;
                    if (Math.Abs(relativeDifference) > _comparisonDefinition.ToleranceValue)
                    {
                        success = false;
                        AddBreak(BreakType.ValueMismatch, key, leftHandSideRowIndex, rightHandSideRowIndex, columnName, leftHandSideValue, rightHandSideValue);
                    }
                    break;
                case ToleranceType.Exact:
                default:
                    if (leftHandSideDouble != rightHandSideDouble)
                    {
                        success = false;
                        AddBreak(BreakType.ValueMismatch, key, leftHandSideRowIndex, rightHandSideRowIndex, columnName, leftHandSideValue, rightHandSideValue);
                    }
                    
                    break;
            }
        }        
        else if (leftHandSideValue != rightHandSideValue)
        {
            // Default to string comparison
            success = false;
            AddBreak(BreakType.ValueMismatch, key, leftHandSideRowIndex, rightHandSideRowIndex, columnName, leftHandSideValue, rightHandSideValue);
        }

        return success;
    }

    void AddBreak(BreakType breakType,
        string breakKey,
        int leftHandSideRowIndex,
        int rightHandSideRowIndex,
        string columnName,
        string leftHandSideValue,
        string rightHandSideValue)
    {
        foreach (var exclusion in _comparisonDefinition.KeyExclusions)
        {
            if (Regex.IsMatch(breakKey, exclusion))
            {
                return;
            }
        }

        _breaks.Add(new BreakDetail(breakType, breakKey, leftHandSideRowIndex, rightHandSideRowIndex, columnName, leftHandSideValue, rightHandSideValue));
    }

    /// <summary>
    /// Gets the column indexes of the key columns
    /// </summary>
    /// <param name="headerRow"></param>
    /// <returns></returns>
    /// <exception cref="ComparisonException">Thrown if no columns found matching keys</exception>
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

        for (int columnIndex = 0; columnIndex < headerRow.Length; columnIndex++)
        {
            if (_comparisonDefinition.ExcludedColumns != null && _comparisonDefinition.ExcludedColumns.Contains(headerRow[columnIndex]))
            {
                excludedColumns.Add(columnIndex);
            }
        }

        return excludedColumns;
    }
}
