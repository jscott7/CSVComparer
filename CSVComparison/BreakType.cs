using System;
using System.Collections.Generic;
using System.Text;

namespace CSVComparison
{
    public enum BreakType
    {
        Match,
        ColumnsDifferent,
        RowInReferenceNotInTarget,
        RowInTargetNotInReference,
        ValueMismatch
    }
}
