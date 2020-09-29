using System;
using System.Collections.Generic;

namespace CSVComparison
{
    public class ComparisonResult
    {
        /// <summary>
        /// Description of reference file (typically Path)
        /// </summary>
        public string ReferenceSource;

        /// <summary>
        /// Description of candidate file (typically Path)
        /// </summary>
        public string CandidateSource;

        /// <summary>
        /// Date the Comparison was made
        /// </summary>
        // TODO: This is not being set correctly
        public DateTime Date;

        /// <summary>
        /// The complete set of breaks between the files
        /// </summary>
        public List<BreakDetail> BreakDetails = new List<BreakDetail>();

        /// <summary>
        /// Number of rows in the reference file
        /// </summary>
        public long NumberOfReferenceRows;

        /// <summary>
        /// Number of rows in the candidate file
        /// </summary>
        public long NumberOfCandidateRows;

        public ComparisonResult(List<BreakDetail> breakDetails)
        {
            BreakDetails = breakDetails;
        }
    }
}
