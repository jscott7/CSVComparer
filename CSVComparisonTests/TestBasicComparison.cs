using NUnit.Framework;
using CSVComparison;
using System;
using System.IO;

namespace CSVComparisonTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestIdenticalFile()
        {
            var testDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");
            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("COL1");
            var csvComparer = new CSVComparer(comparisonDefinition);
            
            var comparisonResult = csvComparer.CompareFiles(testDataFile, testDataFile);

            Assert.AreEqual(0, comparisonResult.BreakDetails.Count);
            Assert.AreEqual(testDataFile, comparisonResult.ReferenceSource);
            Assert.AreEqual(testDataFile, comparisonResult.TargetSource);
        }

        [Test]
        public void TestDifferentValue()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");
            var targetDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVValueBreak.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("COL1");
            var csvComparer = new CSVComparer(comparisonDefinition);

            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, targetDataFile);

            Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
            Assert.AreEqual(BreakType.ValueMismatch, comparisonResult.BreakDetails[0].BreakType);
            Assert.AreEqual("A: B != D", comparisonResult.BreakDetails[0].BreakDescription);
        }

        [Test]
        public void TestDifferentValuePipeDelimited()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVPipe.csv");
            var targetDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVValueBreakPipe.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "|" };
            comparisonDefinition.KeyColumns.Add("COL1");
            var csvComparer = new CSVComparer(comparisonDefinition);

            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, targetDataFile);

            Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
            Assert.AreEqual(BreakType.ValueMismatch, comparisonResult.BreakDetails[0].BreakType);
            Assert.AreEqual("A: B != D", comparisonResult.BreakDetails[0].BreakDescription);
        }

        [Test]
        public void TestDifferentValueRowsBeforeHeader()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVExtraRows.csv");
            var targetDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVValueBreakExtraRows.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = ",", HeaderRowIndex = 2 };
            comparisonDefinition.KeyColumns.Add("COL1");
            var csvComparer = new CSVComparer(comparisonDefinition);

            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, targetDataFile);

            Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
            Assert.AreEqual(BreakType.ValueMismatch, comparisonResult.BreakDetails[0].BreakType);
            Assert.AreEqual("A: B != D", comparisonResult.BreakDetails[0].BreakDescription);
        }

        [Test]
        public void TestExtraRowValue()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");
            var targetDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVExtraRow.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("COL1");
            var csvComparer = new CSVComparer(comparisonDefinition);

            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, targetDataFile);

            Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
            Assert.AreEqual(BreakType.RowInTargetNotInReference, comparisonResult.BreakDetails[0].BreakType);
            Assert.AreEqual("D", comparisonResult.BreakDetails[0].BreakDescription);
        }

        [Test]
        public void TestMissingRowValue()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVExtraRow.csv");
            var targetDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("COL1");
            var csvComparer = new CSVComparer(comparisonDefinition);

            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, targetDataFile);

            Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
            Assert.AreEqual(BreakType.RowInReferenceNotInTarget, comparisonResult.BreakDetails[0].BreakType);
            Assert.AreEqual("D", comparisonResult.BreakDetails[0].BreakDescription);
        }

        [Test]
        public void TestDifferentColumns()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");
            var targetDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVExtraColumn.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("COL1");
            var csvComparer = new CSVComparer(comparisonDefinition);

            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, targetDataFile);

            Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
            Assert.AreEqual(BreakType.ColumnsDifferent, comparisonResult.BreakDetails[0].BreakType);
            Assert.AreEqual("Reference has 3 columns, Target has 4 columns", comparisonResult.BreakDetails[0].BreakDescription);
        }


    }

}