using ComparisonRunner;
using NUnit.Framework;
using System;
using System.IO;

namespace CSVComparisonTests;

public class TestComparisonUtils
{
    [Test]
    public void TestSingleFileComparison()
    {       
        Console.WriteLine($"{Directory.GetCurrentDirectory()} - {Path.GetTempPath()} - {Path.GetTempFileName()}"); // + TestData

        var expectedPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "ComplexLeftHandSideFile.csv");
        var rightHandSidePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "ComplexRightHandSideFile.csv");

        var outputPath = Path.Combine(Path.GetTempPath(), "Output");
        var comparisonDefinitionPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Definition.xml");

        try
        {
            ComparisonUtils.RunSingleComparison(comparisonDefinitionPath, expectedPath, rightHandSidePath, "");
            Assert.That(!File.Exists(Path.Combine(outputPath, "ComparisonResults.BREAKS.csv")));

            ComparisonUtils.RunSingleComparison(comparisonDefinitionPath, expectedPath, rightHandSidePath, outputPath);
            Assert.IsTrue(File.Exists(Path.Combine(outputPath, "ComparisonResults.BREAKS.csv")));
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }
        }
    }

    [TestCase("ComplexRightHandSideFile.csv")]
    [TestCase("ComplexLeftHandSideFile.csv")]
    public void TestFolderComparison(string rightHandSideFileName)
    {
        Console.WriteLine($"{Directory.GetCurrentDirectory()} - {Path.GetTempPath()} - {Path.GetTempFileName()}"); // + TestData

        var expectedPath = Path.Combine(Path.GetTempPath(), "ExpectedTest");
        if (!Directory.Exists(expectedPath))
        {
            Directory.CreateDirectory(expectedPath);
        }

        var rightHandSidePath = Path.Combine(Path.GetTempPath(), "RightHandSideTest");
        if (!Directory.Exists(rightHandSidePath))
        {
            Directory.CreateDirectory(rightHandSidePath);
        }

        var outputPath = Path.Combine(Path.GetTempPath(), "Output");

        var expectedFile = Path.Combine(expectedPath, "ComplexLeftHandSideFile.csv");
        var rightHandSideFile = Path.Combine(rightHandSidePath, rightHandSideFileName);
        File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "ComplexLeftHandSideFile.csv"), expectedFile, true);
        File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "TestData", rightHandSideFileName), rightHandSideFile, true);

        var comparisonDefinitionPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "MultipleDefinition.xml");
        try
        {
            // Run with no output path
            ComparisonUtils.RunDirectoryComparison(comparisonDefinitionPath, expectedPath, rightHandSidePath, "");
            Assert.That(!File.Exists(Path.Combine(outputPath, "Reconciliation-Results-Test.BREAKS.csv")));

            ComparisonUtils.RunDirectoryComparison(comparisonDefinitionPath, expectedPath, rightHandSidePath, outputPath);

            if (rightHandSideFileName != "ComplexLeftHandSideFile.csv")
            {
                Assert.IsTrue(File.Exists(Path.Combine(outputPath, "Reconciliation-Results-Test.BREAKS.csv")));
            }
            else
            {
                Assert.IsTrue(File.Exists(Path.Combine(outputPath, "Reconciliation-Results-Test.csv")));
            }
        }
        finally
        {
            if (Directory.Exists(expectedPath))
            {
                Directory.Delete(expectedPath, true);
            }

            if (Directory.Exists(rightHandSidePath))
            {
                Directory.Delete(rightHandSidePath, true);
            }

            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }
        }
    }
}
