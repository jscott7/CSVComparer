namespace CSVComparison
{
    public class BreakDetail
    {
        public BreakType BreakType;

        public string BreakDescription;

        public string BreakKey;

        public int ReferenceRow;

        public int CandidateRow;

        public string ReferenceValue;

        public string CandidateValue;

        public BreakDetail() { }

        public BreakDetail(
            BreakType breakType,         
            string breakKey, 
            int referenceRow, 
            int candidateRow, 
            string referenceValue, 
            string candidateValue)
        {
            BreakType = breakType;
            BreakKey = breakKey;
            ReferenceRow = referenceRow;
            CandidateRow = candidateRow;
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
