namespace CSVComparison;

public enum BreakType
{
    Match,
    ColumnsDifferent,
    RowInReferenceNotInCandidate,
    RowInCandidateNotInReference,
    ValueMismatch,
    ProcessFailure
}
