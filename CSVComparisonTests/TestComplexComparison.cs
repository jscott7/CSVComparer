using CSVComparison;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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

            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, targetDataFile);

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

            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, targetDataFile);

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

            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, targetDataFile);

            Assert.AreEqual(3, comparisonResult.BreakDetails.Count);

            var comparisonResult2 = csvComparer.CompareFiles(referenceDataFile, referenceDataFile);
            Assert.AreEqual(0, comparisonResult2.BreakDetails.Count);
        }

        [Test]
        public void TestWithExcludedColumn()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexReferenceFile.csv");
            var targetDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexTargetFile.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("ABC");
            comparisonDefinition.KeyColumns.Add("DEF");

            comparisonDefinition.ExcludedColumns = new List<string> () { "AValueColumn" };

            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, targetDataFile);

            Assert.AreEqual(2, comparisonResult.BreakDetails.Count);
        }

        [Test]
        public void TestNonUniqueKey()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexReferenceFile.csv");
            var targetDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexCandidateFile.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("ABC");
            comparisonDefinition.KeyColumns.Add("AnotherColumn");

            var csvComparer = new CSVComparer(comparisonDefinition);
            var exception = Assert.Throws<AggregateException>(delegate { csvComparer.CompareFiles(referenceDataFile, targetDataFile); });
        
            Assert.AreEqual("Reference orphan A:x already exists. This usually means the key columns do not define unique rows.", exception.InnerException.Message);
        }
    }
}
