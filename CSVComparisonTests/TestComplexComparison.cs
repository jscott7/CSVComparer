using CSVComparison;
using NUnit.Framework;
using System;
using System.IO;

namespace CSVComparisonTests
{
    public class TestComplexComparison
    {
        [Test]
        public void TestFilesWithMultipleBreaks()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexReferenceFile.csv");
            var targetDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexTargetFile.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("ABC");
            comparisonDefinition.KeyColumns.Add("DEF");       

            var csvComparer = new CSVComparer();
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, targetDataFile, comparisonDefinition);

            Assert.AreEqual(3, comparisonResult.BreakDetails.Count);
        }
    }
}
