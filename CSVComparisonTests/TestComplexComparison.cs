using CSVComparison;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace CSVComparisonTests;

public class TestComplexComparison
{
    [Test]
    public void TestFilesWithMultipleBreaks()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexLeftHandSideFile.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexFile.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("ABC");
        comparisonDefinition.KeyColumns.Add("DEF");       

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(3));
    }

    [Test]
    public void TestFilesWithDifferentColumns()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexLeftHandSideFile.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexFileDifferentColumns.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("ABC");
        comparisonDefinition.KeyColumns.Add("DEF");

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(1));
        Assert.That(comparisonResult.BreakDetails[0].BreakDescription, Is.EqualTo("LeftHandSide has 4 columns, RightHandSide has 5 columns"));
        Assert.That(comparisonResult.BreakDetails[0].BreakType, Is.EqualTo(BreakType.ColumnsDifferent));
    }

    [Test]
    public void TestReusingSameComparisonObject()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexLeftHandSideFile.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexFile.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("ABC");
        comparisonDefinition.KeyColumns.Add("DEF");

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(3));

        var comparisonResult2 = csvComparer.CompareFiles(leftHandSideDataFile, leftHandSideDataFile);
        Assert.That(comparisonResult2.BreakDetails.Count, Is.EqualTo(0));
    }

    [Test]
    public void TestWithExcludedColumn()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexLeftHandSideFile.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexFile.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("ABC");
        comparisonDefinition.KeyColumns.Add("DEF");

        comparisonDefinition.ExcludedColumns = new List<string> () { "AValueColumn" };

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(2));
    }

    [Test]
    public void TestNonUniqueKey()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexLeftHandSideFile.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexRightHandSideFile.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("ABC");
        comparisonDefinition.KeyColumns.Add("AnotherColumn");

        var csvComparer = new CSVComparer(comparisonDefinition);
        var exception = Assert.Throws<AggregateException>( () => csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile) );
    
        Assert.That(exception.InnerException.Message, Is.EqualTo("Orphan key: A:x already exists. This usually means the key columns do not define unique rows."));
    }

    [Test]    
    public void TestOrphanExclusions()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexLeftHandSideFile.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexFile.csv");
        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("ABC");
        comparisonDefinition.KeyColumns.Add("DEF");
        comparisonDefinition.KeyColumns.Add("AnotherColumn");

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(4), "Number of breaks without exclusions");

        comparisonDefinition.OrphanExclusions = new List<string> { "NewData" };
        csvComparer = new CSVComparer(comparisonDefinition);
        comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(3), "Number of breaks with single orphan value exclusion");
    }

    [Test]
    public void TestKeyExclusions()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexLeftHandSideFile.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ComplexFileKeyExclusion.csv");
        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("ABC");
        comparisonDefinition.KeyColumns.Add("DEF");
        comparisonDefinition.KeyColumns.Add("AnotherColumn");

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(5), "Number of breaks without exclusions");

        comparisonDefinition.KeyExclusions = new List<string> { "TestData" };
        csvComparer = new CSVComparer(comparisonDefinition);
        comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);

        Assert.That(comparisonResult.BreakDetails.Count, Is.EqualTo(4), "Number of breaks with single orphan value exclusion");
    }
}
