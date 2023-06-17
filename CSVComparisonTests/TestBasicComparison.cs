using NUnit.Framework;
using CSVComparison;
using System;
using System.IO;

namespace CSVComparisonTests;

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

        Assert.AreEqual(0, comparisonResult.BreakDetails.Count, "Invalid number of breaks");
        Assert.AreEqual(2, comparisonResult.NumberOfLeftHandSideRows, "Invalid number of leftHandSide rows");
        Assert.AreEqual(2, comparisonResult.NumberOfRightHandSideRows, "Invalid number of rightHandSide rows");
        Assert.AreEqual(testDataFile, comparisonResult.LeftHandSideSource, "Invalid leftHandSide source name");
        Assert.AreEqual(testDataFile, comparisonResult.RightHandSideSource, "Invalid rightHandSide source name");
    }

    [Test]
    public void TestDifferentValue()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVValueBreak.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("COL1");

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
        Assert.AreEqual(BreakType.ValueMismatch, comparisonResult.BreakDetails[0].BreakType);
        Assert.AreEqual("Key:A, LeftHandSide Row:2, Value:B != RightHandSide Row:2, Value:D", comparisonResult.BreakDetails[0].BreakDescription);
    }

    [Test]
    public void TestDifferentValuePipeDelimited()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVPipe.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVValueBreakPipe.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "|" };
        comparisonDefinition.KeyColumns.Add("COL1");

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
        Assert.AreEqual(BreakType.ValueMismatch, comparisonResult.BreakDetails[0].BreakType);
        Assert.AreEqual("Key:A, LeftHandSide Row:2, Value:B != RightHandSide Row:2, Value:D", comparisonResult.BreakDetails[0].BreakDescription);
    }

    [Test]
    public void TestDifferentValueRowsBeforeHeader()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVExtraHeaderRows.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVValueBreakExtraHeaderRows.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = ",", HeaderRowIndex = 2 };
        comparisonDefinition.KeyColumns.Add("COL1");

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
        Assert.AreEqual(BreakType.ValueMismatch, comparisonResult.BreakDetails[0].BreakType);
        Assert.AreEqual("Key:A, LeftHandSide Row:4, Value:B != RightHandSide Row:4, Value:D", comparisonResult.BreakDetails[0].BreakDescription);
    }

    [Test]
    public void TestExtraRowValue()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVExtraRow.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("COL1");

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
        Assert.AreEqual(BreakType.RowInRHS_NotInLHS, comparisonResult.BreakDetails[0].BreakType);
        Assert.AreEqual("D", comparisonResult.BreakDetails[0].BreakKey);
    }

    [Test]
    public void TestMissingRowValue()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVExtraRow.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("COL1");

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
        Assert.AreEqual(BreakType.RowInLHS_NotInRHS, comparisonResult.BreakDetails[0].BreakType);
        Assert.AreEqual("D", comparisonResult.BreakDetails[0].BreakKey);
    }

    [Test]
    public void TestDifferentColumns()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVExtraColumn.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("COL1");

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
        Assert.AreEqual(BreakType.ColumnsDifferent, comparisonResult.BreakDetails[0].BreakType);
        Assert.AreEqual("LeftHandSide has 3 columns, RightHandSide has 4 columns", comparisonResult.BreakDetails[0].BreakDescription);
    }

    [Test]
    public void TestMissingFile()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "MissingFile.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("COL1");

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
        Assert.IsTrue(comparisonResult.BreakDetails[0].BreakDescription.StartsWith("Problem loading"));
        Assert.AreEqual(BreakType.ProcessFailure, comparisonResult.BreakDetails[0].BreakType);
    }

    [Test]
    public void TestEmptyFile()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "EmptyFile.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "EmptyFile.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("COL1");

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);
        Assert.AreEqual(0, comparisonResult.BreakDetails.Count, "Should be no breaks for two empty files");
        
        leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");
        var comparisonResult2 = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);
        Assert.AreEqual(2, comparisonResult2.BreakDetails.Count, "All leftHandSide rows should be orphans");
    }

    [Test]
    public void TestAbsoluteComparison()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVWithDouble.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVWithDoubleBreak.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("COL1");
        comparisonDefinition.ToleranceType = ToleranceType.Absolute;
        comparisonDefinition.ToleranceValue = 0.1;

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);
        Assert.AreEqual(2, comparisonResult.BreakDetails.Count, "Absolute tolerance");
        Assert.AreEqual("Key:A, LeftHandSide Row:2, Value:1.0 != RightHandSide Row:2, Value:1.2", comparisonResult.BreakDetails[0].BreakDescription);
        Assert.AreEqual("Key:C, LeftHandSide Row:3, Value:2.5 != RightHandSide Row:3, Value:2.61", comparisonResult.BreakDetails[1].BreakDescription);
    }

    [Test]
    public void TestRelativeToleranceComparison()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVWithDouble.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVWithDoubleBreak.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("COL1");
        comparisonDefinition.ToleranceType = ToleranceType.Relative;
        comparisonDefinition.ToleranceValue = 0.1;

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);
        Assert.AreEqual(1, comparisonResult.BreakDetails.Count, "Relative tolerance");
        Assert.AreEqual("Key:A, LeftHandSide Row:2, Value:1.0 != RightHandSide Row:2, Value:1.2", comparisonResult.BreakDetails[0].BreakDescription);
    }

    [Test]
    public void DefaultExact_Tolerance_DoubleComparison()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVWithDouble.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVWithDoubleBreak.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("COL1");
        comparisonDefinition.ToleranceValue = 1.5;

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(2), "Exact tolerance");
        Assert.That(comparisonResult.BreakDetails[0].BreakDescription, Is.EqualTo("Key:A, LeftHandSide Row:2, Value:1.0 != RightHandSide Row:2, Value:1.2"));
        Assert.That(comparisonResult.BreakDetails[1].BreakDescription, Is.EqualTo("Key:C, LeftHandSide Row:3, Value:2.5 != RightHandSide Row:3, Value:2.61"));

        // Explicitly set Exact
        comparisonDefinition.ToleranceType = ToleranceType.Exact;
        var csvComparerExact = new CSVComparer(comparisonDefinition);
        var comparisonResultExact = csvComparerExact.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.That(comparisonResultExact.BreakDetails.Count, Is.EqualTo(2), "Exact tolerance");
        Assert.That(comparisonResultExact.BreakDetails[0].BreakDescription, Is.EqualTo("Key:A, LeftHandSide Row:2, Value:1.0 != RightHandSide Row:2, Value:1.2"));
        Assert.That(comparisonResultExact.BreakDetails[1].BreakDescription, Is.EqualTo("Key:C, LeftHandSide Row:3, Value:2.5 != RightHandSide Row:3, Value:2.61"));
    }

    [Test]
    public void TestFooter()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVValueExtraFooterRows.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVValueBreakExtraFooterRows.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("COL1");

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.AreEqual(2, comparisonResult.BreakDetails.Count);

        var comparisonDefinition2 = new ComparisonDefinition() { Delimiter = ",", IgnoreInvalidRows = true };
        comparisonDefinition2.KeyColumns.Add("COL1");

        var csvComparer2 = new CSVComparer(comparisonDefinition2);
        var comparisonResult2 = csvComparer2.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.AreEqual(0, comparisonResult2.BreakDetails.Count);
    }

    [Test]
    public void TestOrphanColumnWithCommaInValue()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVCommaInColumn.csv");
        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("COL1");

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);
        Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
    }

    [Test]
    public void TestBreakColumnWithCommaInValue()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVCommaInColumn.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVCommaInColumnBreak.csv");
        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("COL1");

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);
        Assert.AreEqual(1, comparisonResult.BreakDetails.Count);
        Assert.AreEqual("A column without a comma!", comparisonResult.BreakDetails[0].RightHandSideValue);
        Assert.AreEqual("\"A column, with a comma!\"", comparisonResult.BreakDetails[0].LeftHandSideValue);
    }

    [Test]
    public void TestDoubleComparisonInQuotes()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVWithDoubleInQuotes.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSVWithDoubleInQuotesBreak.csv");
        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("COL1");

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        // Default is no tolerance, absolute match
        Assert.AreEqual(2, comparisonResult.BreakDetails.Count, "Exact match test");

        comparisonDefinition.ToleranceValue = 0.1;
        comparisonDefinition.ToleranceType = ToleranceType.Absolute;
        csvComparer = new CSVComparer(comparisonDefinition);
        comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.AreEqual(1, comparisonResult.BreakDetails.Count, "Absolute Tolerance 0.1 test");

        comparisonDefinition.ToleranceValue = 0.25;
        comparisonDefinition.ToleranceType = ToleranceType.Relative;
        csvComparer = new CSVComparer(comparisonDefinition);
        comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.AreEqual(0, comparisonResult.BreakDetails.Count, "Relative Tolerance 0.25 test");
    }
}