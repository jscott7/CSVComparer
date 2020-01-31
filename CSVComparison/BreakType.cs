namespace CSVComparison
{
    public enum BreakType
    {
        Match,
        ColumnsDifferent,
        RowInReferenceNotInTarget,
        RowInTargetNotInReference,
        ValueMismatch,
        ProcessFailure
    }
}
