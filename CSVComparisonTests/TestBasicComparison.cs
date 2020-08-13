using NUnit.Framework;
using CSVComparison;
using System;
using System.IO;

namespace CSVComparisonTests
{
    public class TestBasicComparison
    {
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
            Assert.AreEqual(testDataFile, comparisonResult.CandidateSource);
        }

        [Test]
        public void TestDifferentValue()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");
            var CandidateDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVValueBreak.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("COL1");
    
            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, CandidateDataFile);

            Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
            Assert.AreEqual(BreakType.ValueMismatch, comparisonResult.BreakDetails[0].BreakType);
            Assert.AreEqual("Key:A, Reference Row:1, Value:B != Candidate Row:1, Value:D", comparisonResult.BreakDetails[0].BreakDescription);
        }

        [Test]
        public void TestDifferentValuePipeDelimited()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVPipe.csv");
            var CandidateDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVValueBreakPipe.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "|" };
            comparisonDefinition.KeyColumns.Add("COL1");

            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, CandidateDataFile);

            Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
            Assert.AreEqual(BreakType.ValueMismatch, comparisonResult.BreakDetails[0].BreakType);
            Assert.AreEqual("Key:A, Reference Row:1, Value:B != Candidate Row:1, Value:D", comparisonResult.BreakDetails[0].BreakDescription);
        }

        [Test]
        public void TestDifferentValueRowsBeforeHeader()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVExtraHeaderRows.csv");
            var CandidateDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVValueBreakExtraHeaderRows.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = ",", HeaderRowIndex = 2 };
            comparisonDefinition.KeyColumns.Add("COL1");

            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, CandidateDataFile);

            Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
            Assert.AreEqual(BreakType.ValueMismatch, comparisonResult.BreakDetails[0].BreakType);
            Assert.AreEqual("Key:A, Reference Row:3, Value:B != Candidate Row:3, Value:D", comparisonResult.BreakDetails[0].BreakDescription);
        }

        [Test]
        public void TestExtraRowValue()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");
            var CandidateDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVExtraRow.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("COL1");
 
            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, CandidateDataFile);

            Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
            Assert.AreEqual(BreakType.RowInCandidateNotInReference, comparisonResult.BreakDetails[0].BreakType);
            Assert.AreEqual("D", comparisonResult.BreakDetails[0].BreakKey);
        }

        [Test]
        public void TestMissingRowValue()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVExtraRow.csv");
            var CandidateDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("COL1");
  
            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, CandidateDataFile);

            Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
            Assert.AreEqual(BreakType.RowInReferenceNotInCandidate, comparisonResult.BreakDetails[0].BreakType);
            Assert.AreEqual("D", comparisonResult.BreakDetails[0].BreakKey);
        }

        [Test]
        public void TestDifferentColumns()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");
            var CandidateDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVExtraColumn.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("COL1");
    
            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, CandidateDataFile);

            Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
            Assert.AreEqual(BreakType.ColumnsDifferent, comparisonResult.BreakDetails[0].BreakType);
            Assert.AreEqual("Reference has 3 columns, Candidate has 4 columns", comparisonResult.BreakDetails[0].BreakDescription);
        }

        [Test]
        public void TestMissingFile()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");
            var CandidateDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "MissingFile.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("COL1");

            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, CandidateDataFile);

            Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
            Assert.IsTrue(comparisonResult.BreakDetails[0].BreakDescription.StartsWith("Problem loading"));
            Assert.AreEqual(BreakType.ProcessFailure, comparisonResult.BreakDetails[0].BreakType);
        }

        [Test]
        public void TestAbsoluteComparison()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVWithDouble.csv");
            var CandidateDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVWithDoubleBreak.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("COL1");
            comparisonDefinition.ToleranceType = ToleranceType.Absolute;
            comparisonDefinition.ToleranceValue = 0.1;

            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, CandidateDataFile);
            Assert.AreEqual(2, comparisonResult.BreakDetails.Count, "Absolute tolerance");
            Assert.AreEqual("Key:A, Reference Row:1, Value:1.0 != Candidate Row:1, Value:1.2", comparisonResult.BreakDetails[0].BreakDescription);
            Assert.AreEqual("Key:C, Reference Row:2, Value:2.5 != Candidate Row:2, Value:2.61", comparisonResult.BreakDetails[1].BreakDescription);
        }

        [Test]
        public void TestRelativeToleranceComparison()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVWithDouble.csv");
            var CandidateDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVWithDoubleBreak.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("COL1");
            comparisonDefinition.ToleranceType = ToleranceType.Relative;
            comparisonDefinition.ToleranceValue = 0.1;

            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, CandidateDataFile);
            Assert.AreEqual(1, comparisonResult.BreakDetails.Count, "Relative tolerance");
            Assert.AreEqual("Key:A, Reference Row:1, Value:1.0 != Candidate Row:1, Value:1.2", comparisonResult.BreakDetails[0].BreakDescription);
        }

        [Test]
        public void TestExactDoubleComparison()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVWithDouble.csv");
            var CandidateDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVWithDoubleBreak.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("COL1");
            comparisonDefinition.ToleranceValue = 0.1;

            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, CandidateDataFile);
            Assert.AreEqual(2, comparisonResult.BreakDetails.Count, "Absolute tolerance");
            Assert.AreEqual("Key:A, Reference Row:1, Value:1.0 != Candidate Row:1, Value:1.2", comparisonResult.BreakDetails[0].BreakDescription);
            Assert.AreEqual("Key:C, Reference Row:2, Value:2.5 != Candidate Row:2, Value:2.61", comparisonResult.BreakDetails[1].BreakDescription);
        }

        [Test]
        public void TestFooter()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVValueExtraFooterRows.csv");
            var candidateDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVValueBreakExtraFooterRows.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("COL1");

            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, candidateDataFile);

            Assert.AreEqual(2, comparisonResult.BreakDetails.Count);

            var comparisonDefinition2 = new ComparisonDefinition() { Delimiter = ",", IgnoreInvalidRows = true };
            comparisonDefinition2.KeyColumns.Add("COL1");

            var csvComparer2 = new CSVComparer(comparisonDefinition2);
            var comparisonResult2 = csvComparer2.CompareFiles(referenceDataFile, candidateDataFile);

            Assert.AreEqual(0, comparisonResult2.BreakDetails.Count);
        }
    }
}