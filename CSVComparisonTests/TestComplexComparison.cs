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

        [Test]
        public void TestFilesWithDifferentColumns()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexReferenceFile.csv");
            var targetDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexTargetFileDifferentColumns.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("ABC");
            comparisonDefinition.KeyColumns.Add("DEF");

            var csvComparer = new CSVComparer();
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, targetDataFile, comparisonDefinition);

            Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
            Assert.AreEqual("Reference has 4 columns, Candidate has 5 columns", comparisonResult.BreakDetails[0].BreakDescription);
            Assert.AreEqual(BreakType.ColumnsDifferent, comparisonResult.BreakDetails[0].BreakType);
        }

        [Test]
        public void TestReusingSameComparisonObject()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexReferenceFile.csv");
            var targetDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexTargetFile.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("ABC");
            comparisonDefinition.KeyColumns.Add("DEF");

            var csvComparer = new CSVComparer();
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, targetDataFile, comparisonDefinition);

            Assert.AreEqual(3, comparisonResult.BreakDetails.Count);

            var comparisonResult2 = csvComparer.CompareFiles(referenceDataFile, referenceDataFile, comparisonDefinition);
            Assert.AreEqual(0, comparisonResult.BreakDetails.Count);
        }
    }
}
