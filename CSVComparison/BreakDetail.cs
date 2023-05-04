namespace CSVComparison;

/// <summary>
/// Records information about an individual "Break" between two CSV files
/// </summary>
public class BreakDetail
{
    /// <summary>
    /// The type of Break
    /// </summary>
    public BreakType BreakType;

    /// <summary>
    /// A single line description of the break
    /// </summary>
    public string BreakDescription;

    /// <summary>
    /// The key of the row in the CSV file
    /// </summary>
    public string BreakKey;

    /// <summary>
    /// The index of the row in the leftHandSide CSV file
    /// </summary>
    public int LeftHandSideRow;

    /// <summary>
    /// The index of the row in the rightHandSide CSV file
    /// </summary>
    public int RightHandSideRow;

    /// <summary>
    /// The name of the column of the mismatching data. Will be blank if the row is an orphan
    /// </summary>
    public string Column;

    /// <summary>
    /// The value of mismatching data. Will be blank if the row is an orphan
    /// </summary>
    public string LeftHandSideValue;

    /// <summary>
    /// The value of mismatching data. Will be blank if the row is an orphan
    /// </summary>
    public string RightHandSideValue;

    public BreakDetail() { }

    public BreakDetail(
        BreakType breakType,         
        string breakKey, 
        int leftHandSideRow, 
        int rightHandSideRow, 
        string column,
        string leftHandSideValue, 
        string rightHandSideValue)
    {
        BreakType = breakType;
        BreakKey = breakKey;
        LeftHandSideRow = leftHandSideRow;
        RightHandSideRow = rightHandSideRow;
        Column = column;
        LeftHandSideValue = leftHandSideValue;
        RightHandSideValue = rightHandSideValue;

        BreakDescription = $"Key:{BreakKey}, LeftHandSide Row:{LeftHandSideRow}, Value:{LeftHandSideValue} != RightHandSide Row:{RightHandSideRow}, Value:{RightHandSideValue}";
    }

    public override string ToString()
    {
        return $"Break Type: {BreakType}. Description: {BreakDescription}";
    }
}
