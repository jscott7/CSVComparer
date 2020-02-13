using System;
using System.Collections.Generic;
using System.Text;

namespace CSVComparison
{
    public class ComparisonResult
    {
        public string ReferenceSource;

        public string CandidateSource;

        public DateTime Date;

        public List<BreakDetail> BreakDetails = new List<BreakDetail>();

        public ComparisonResult(List<BreakDetail> breakDetails)
        {
            BreakDetails = breakDetails;
        }
    }
}
