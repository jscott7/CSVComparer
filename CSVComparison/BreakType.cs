namespace CSVComparison;

public enum BreakType
{
    Match,
    ColumnsDifferent,
    RowInLHS_NotInRHS,
    RowInRHS_NotInLHS,
    ValueMismatch,
    ProcessFailure
}
