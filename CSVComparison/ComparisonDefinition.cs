using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

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
        [XmlArrayItem("Column")]
        public List<string> KeyColumns;

        /// <summary>
        /// The index of the header row
        /// </summary>
        public int HeaderRowIndex;

        /// <summary>
        /// The Tolerance to be used for numeric value columns
        /// </summary>
        public double ToleranceValue;  

        /// <summary>
        /// How tolerance value is applied, e.g. Absolute or Relative
        /// </summary>
        public ToleranceType ToleranceType;
        
        public ComparisonDefinition()
        {
            KeyColumns = new List<string>();
        }
    }
}
