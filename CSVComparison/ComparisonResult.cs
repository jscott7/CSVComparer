using System;
using System.Collections.Generic;

namespace CSVComparison;

public class ComparisonResult
{
    /// <summary>
    /// Description of leftHandSide file (typically Path)
    /// </summary>
    public string LeftHandSideSource;

    /// <summary>
    /// Description of rightHandSide file (typically Path)
    /// </summary>
    public string RightHandSideSource;

    /// <summary>
    /// Date the Comparison was made
    /// </summary>
    public DateTime Date;

    /// <summary>
    /// The complete set of breaks between the files
    /// </summary>
    public List<BreakDetail> BreakDetails = new List<BreakDetail>();

    /// <summary>
    /// Number of rows in the leftHandSide file
    /// </summary>
    public long NumberOfLeftHandSideRows;

    /// <summary>
    /// Number of rows in the rightHandSide file
    /// </summary>
    public long NumberOfRightHandSideRows;

    /// <summary>
    /// The Columns used in the Key concatenated by ':'. In same order as Key values
    /// </summary>
    public string KeyDefinition;
    public ComparisonResult(List<BreakDetail> breakDetails)
    {
        BreakDetails = breakDetails;
    }
}
