namespace CSVComparison
{
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
        /// The index of the row in the reference CSV file
        /// </summary>
        public int ReferenceRow;

        /// <summary>
        /// The index of the row in the candidate CSV file
        /// </summary>
        public int CandidateRow;

        /// <summary>
        /// The name of the column of the mismatching data. Will be blank if the row is an orphan
        /// </summary>
        public string Column;

        /// <summary>
        /// The value of mismatching data. Will be blank if the row is an orphan
        /// </summary>
        public string ReferenceValue;

        /// <summary>
        /// The value of mismatching data. Will be blank if the row is an orphan
        /// </summary>
        public string CandidateValue;

        public BreakDetail() { }

        public BreakDetail(
            BreakType breakType,         
            string breakKey, 
            int referenceRow, 
            int candidateRow, 
            string column,
            string referenceValue, 
            string candidateValue)
        {
            BreakType = breakType;
            BreakKey = breakKey;
            ReferenceRow = referenceRow;
            CandidateRow = candidateRow;
            Column = column;
            ReferenceValue = referenceValue;
            CandidateValue = candidateValue;

            BreakDescription = $"Key:{BreakKey}, Reference Row:{ReferenceRow}, Value:{ReferenceValue} != Candidate Row:{CandidateRow}, Value:{CandidateValue}";
        }

        public override string ToString()
        {
            return $"Break Type: {BreakType}. Description {BreakDescription}";
        }
    }
}
