using CSVComparison;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace CSVComparisonTests
{
    public class TestConfiguration
    {
        [Test]
        public void TestConfiguarationSerialization()
        {
            var comparisonDefinition = new ComparisonDefinition()
            {
                Delimiter = ",",
                HeaderRowIndex = 1,
                ToleranceValue = 0.1
            };

            comparisonDefinition.KeyColumns = new List<string>() { "ABC", "DEF" };

            var xmlSerializer = new XmlSerializer(typeof(ComparisonDefinition));

            var tempFile = Path.GetTempFileName();
            using (var fs = new FileStream(tempFile, FileMode.Create))
            using (var writer = new XmlTextWriter(fs, Encoding.Unicode))
            {
                // Serialize using the XmlTextWriter.
                xmlSerializer.Serialize(writer, comparisonDefinition);
            }

            var xmlDocument = new XmlDocument();
            xmlDocument.Load(tempFile);

            var deserializedDefinition = (ComparisonDefinition)xmlSerializer.Deserialize(new XmlNodeReader((XmlNode)xmlDocument.DocumentElement));

            Assert.AreEqual(comparisonDefinition.Delimiter, deserializedDefinition.Delimiter, "Delimiter");
            Assert.AreEqual(comparisonDefinition.HeaderRowIndex, deserializedDefinition.HeaderRowIndex, "HeaderRowIndex");
            Assert.AreEqual(comparisonDefinition.ToleranceType, deserializedDefinition.ToleranceType, "ToleranceType");
            Assert.AreEqual(comparisonDefinition.ToleranceValue, deserializedDefinition.ToleranceValue, "ToleranceValue");
            Assert.AreEqual(comparisonDefinition.KeyColumns.Count, deserializedDefinition.KeyColumns.Count, "KeyColumns count");
        }
    }
}
