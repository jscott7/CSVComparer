using System.Collections.Generic;

namespace CSVComparison
{
    public class ComparisonDefinition
    {
        /// <summary>
        /// The delimiter for CSV, can be a char ',' or '|', but complex delimiter also supported, "==" 
        /// </summary>
        public string Delimiter;

        /// <summary>
        /// The names of the Columns used to uniquely identify a row
        /// </summary>
        public List<string> KeyColumns;

        /// <summary>
        /// The index of the header row
        /// </summary>
        public int HeaderRowIndex;

        /// <summary>
        /// The index of the first non-data row at the end of the file. All rows after this will be ignored
        /// </summary>
        public int TrailerRowIndex;

        /// <summary>
        /// The Tolerance to be used for numeric value columns
        /// </summary>
        public double ToleranceValue;  
        
        public ComparisonDefinition()
        {
            KeyColumns = new List<string>();
        }
    }
}
