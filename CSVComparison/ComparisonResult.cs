using System;
using System.Collections.Generic;

namespace CSVComparison
{
    public class ComparisonResult
    {
        public string ReferenceSource;

        public string CandidateSource;

        public DateTime Date;

        public List<BreakDetail> BreakDetails = new List<BreakDetail>();

        public long NumberOfReferenceRows;

        public long NumberOfCandidateRows;

        public ComparisonResult(List<BreakDetail> breakDetails)
        {
            BreakDetails = breakDetails;
        }
    }
}
