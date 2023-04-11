using System.Collections.Generic;
using System.Xml.Serialization;

namespace CSVComparison;

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
    /// Flag to ignore rows with a different number of columns, typically Footer rows
    /// </summary>
    public bool IgnoreInvalidRows;

    /// <summary>
    /// How tolerance value is applied, e.g. Absolute or Relative
    /// </summary>
    public ToleranceType ToleranceType;

    /// <summary>
    /// The names of Columns to be excluded from the comparison
    /// </summary>
    [XmlArrayItem("Column")]
    public List<string> ExcludedColumns;

    /// <summary>
    /// Regex Patterns for excluding orphan keys from the list of breaks
    /// </summary>
    [XmlArrayItem("ExclusionPattern")]
    public List<string> OrphanExclusions = new List<string>();
   
    /// <summary>
    /// Regex Patterns for excluding orphan keys from the list of breaks
    /// </summary>
    [XmlArrayItem("ExclusionPattern")]
    public List<string> KeyExclusions = new List<string>();

    public ComparisonDefinition()
    {
        KeyColumns = new List<string>();
    }
}
