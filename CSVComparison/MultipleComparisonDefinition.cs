using System.Collections.Generic;
using System.Xml.Serialization;

namespace CSVComparison
{
    public class MultipleComparisonDefinition
    {
        [XmlArrayItem("Comparison")]
        public List<FileComparisonDefinition> FileComparisonDefinitions;
    }
}
