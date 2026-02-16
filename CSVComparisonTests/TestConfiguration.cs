using CSVComparison;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace CSVComparisonTests;

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

        Assert.That(deserializedDefinition.Delimiter, Is.EqualTo(comparisonDefinition.Delimiter), "Delimiter");
        Assert.That(deserializedDefinition.HeaderRowIndex, Is.EqualTo(comparisonDefinition.HeaderRowIndex), "HeaderRowIndex");
        Assert.That(deserializedDefinition.ToleranceType, Is.EqualTo(comparisonDefinition.ToleranceType), "ToleranceType");
        Assert.That(deserializedDefinition.ToleranceValue, Is.EqualTo(comparisonDefinition.ToleranceValue), "ToleranceValue");
        Assert.That(deserializedDefinition.KeyColumns.Count, Is.EqualTo(comparisonDefinition.KeyColumns.Count), "KeyColumns count");
    }

    [Test]
    public void TestInvalidKeyColumns()
    {
        var comparisonDefinition = new ComparisonDefinition()
        {
            Delimiter = ",",
            HeaderRowIndex = 0,
            ToleranceValue = 0.1
        };

        comparisonDefinition.KeyColumns = new List<string>() { "A", "B" };

        var testDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");
        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(testDataFile, testDataFile);

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(2));
        Assert.That(comparisonResult.BreakDetails[0].BreakType, Is.EqualTo(BreakType.ProcessFailure));
        Assert.That(comparisonResult.BreakDetails[0].BreakDescription.Contains("No columns match the key columns defined in configuration"), Is.True);
    }

    [Test]
    public void TestMultipleComparisonDefinition()
    {
        var comparisonDefinition = new ComparisonDefinition()
        {
            Delimiter = ",",
            HeaderRowIndex = 1,
            ToleranceValue = 0.1
        };

        comparisonDefinition.KeyColumns = new List<string>() { "ABC", "DEF" };
        var fileComparisonDefinition = new FileComparisonDefinition()
        {
            Key = "Key",
            FilePattern = "*.txt",
            ComparisonDefinition = comparisonDefinition
        };

        var multiComparisonDefinition = new MultipleComparisonDefinition()
        {
            FileComparisonDefinitions = new List<FileComparisonDefinition> { fileComparisonDefinition }
        };

        var xmlSerializer = new XmlSerializer(typeof(MultipleComparisonDefinition));
        var tempFile = Path.GetTempFileName();
        using (var fs = new FileStream(tempFile, FileMode.Create))
        using (var writer = new XmlTextWriter(fs, Encoding.Unicode))
        {
            // Serialize using the XmlTextWriter.
            xmlSerializer.Serialize(writer, multiComparisonDefinition);
        }

        var xmlDocument = new XmlDocument();
        xmlDocument.Load(tempFile);

        var deserializedDefinition = (MultipleComparisonDefinition)xmlSerializer.Deserialize(new XmlNodeReader((XmlNode)xmlDocument.DocumentElement));

        Assert.That(deserializedDefinition.FileComparisonDefinitions.Count, Is.EqualTo(1));
    }
}
