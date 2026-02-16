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

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(0), "Invalid number of breaks");
        Assert.That(comparisonResult.NumberOfLeftHandSideRows, Is.EqualTo(2), "Invalid number of leftHandSide rows");
        Assert.That(comparisonResult.NumberOfRightHandSideRows, Is.EqualTo(2), "Invalid number of rightHandSide rows");
        Assert.That(comparisonResult.LeftHandSideSource, Is.EqualTo(testDataFile), "Invalid leftHandSide source name");
        Assert.That(comparisonResult.RightHandSideSource, Is.EqualTo(testDataFile), "Invalid rightHandSide source name");
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

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(1));
        Assert.That(comparisonResult.BreakDetails[0].BreakType, Is.EqualTo(BreakType.ValueMismatch));
        Assert.That(comparisonResult.BreakDetails[0].BreakDescription, Is.EqualTo("Key:A, LeftHandSide Row:2, Value:B != RightHandSide Row:2, Value:D"));
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

        Assert.That(1, Is.EqualTo(comparisonResult.BreakDetails.Count));
        Assert.That(comparisonResult.BreakDetails[0].BreakType, Is.EqualTo(BreakType.ValueMismatch));
        Assert.That(comparisonResult.BreakDetails[0].BreakDescription, Is.EqualTo("Key:A, LeftHandSide Row:2, Value:B != RightHandSide Row:2, Value:D"));
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

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(1));
        Assert.That(comparisonResult.BreakDetails[0].BreakType, Is.EqualTo(BreakType.ValueMismatch));
        Assert.That(comparisonResult.BreakDetails[0].BreakDescription, Is.EqualTo("Key:A, LeftHandSide Row:4, Value:B != RightHandSide Row:4, Value:D"));
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

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(1));
        Assert.That(comparisonResult.BreakDetails[0].BreakType, Is.EqualTo(BreakType.RowInRHS_NotInLHS));
        Assert.That(comparisonResult.BreakDetails[0].BreakKey, Is.EqualTo("D"));
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

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(1));
        Assert.That(comparisonResult.BreakDetails[0].BreakType, Is.EqualTo(BreakType.RowInLHS_NotInRHS));
        Assert.That(comparisonResult.BreakDetails[0].BreakKey, Is.EqualTo("D"));
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

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(1));
        Assert.That(comparisonResult.BreakDetails[0].BreakType, Is.EqualTo(BreakType.ColumnsDifferent));
        Assert.That(comparisonResult.BreakDetails[0].BreakDescription, Is.EqualTo("LeftHandSide has 3 columns, RightHandSide has 4 columns"));
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

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(1));
        Assert.That(comparisonResult.BreakDetails[0].BreakDescription.StartsWith("Problem loading"), Is.True);
        Assert.That(comparisonResult.BreakDetails[0].BreakType, Is.EqualTo(BreakType.ProcessFailure));
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
        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(0), "Should be no breaks for two empty files");
        
        leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "SimpleCSV.csv");
        var comparisonResult2 = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);
        Assert.That(comparisonResult2.BreakDetails.Count, Is.EqualTo(2), "All leftHandSide rows should be orphans");
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
        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(2), "Absolute tolerance");
        Assert.That(comparisonResult.BreakDetails[0].BreakDescription, Is.EqualTo("Key:A, LeftHandSide Row:2, Value:1.0 != RightHandSide Row:2, Value:1.2"));
        Assert.That(comparisonResult.BreakDetails[1].BreakDescription, Is.EqualTo("Key:C, LeftHandSide Row:3, Value:2.5 != RightHandSide Row:3, Value:2.61"));
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
        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(1), "Relative tolerance");
        Assert.That(comparisonResult.BreakDetails[0].BreakDescription, Is.EqualTo("Key:A, LeftHandSide Row:2, Value:1.0 != RightHandSide Row:2, Value:1.2"));
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

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(2));

        var comparisonDefinition2 = new ComparisonDefinition() { Delimiter = ",", IgnoreInvalidRows = true };
        comparisonDefinition2.KeyColumns.Add("COL1");

        var csvComparer2 = new CSVComparer(comparisonDefinition2);
        var comparisonResult2 = csvComparer2.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.That(comparisonResult2.BreakDetails.Count, Is.EqualTo(0));
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
        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(1));
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
        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(1));
        Assert.That(comparisonResult.BreakDetails[0].RightHandSideValue, Is.EqualTo("A column without a comma!"));
        Assert.That(comparisonResult.BreakDetails[0].LeftHandSideValue, Is.EqualTo("\"A column, with a comma!\""));
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
        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(2), "Exact match test");

        comparisonDefinition.ToleranceValue = 0.1;
        comparisonDefinition.ToleranceType = ToleranceType.Absolute;
        csvComparer = new CSVComparer(comparisonDefinition);
        comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(1), "Absolute Tolerance 0.1 test");

        comparisonDefinition.ToleranceValue = 0.25;
        comparisonDefinition.ToleranceType = ToleranceType.Relative;
        csvComparer = new CSVComparer(comparisonDefinition);
        comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(0), "Relative Tolerance 0.25 test");
    }
}