using ComparisonRunner;
using NUnit.Framework;
using System;
using System.IO;

namespace CSVComparisonTests
{
    public class TestComparisonUtils
    {
        [Test]
        public void TestSingleFileComparison()
        {       
            Console.WriteLine($"{Directory.GetCurrentDirectory()} - {Path.GetTempPath()} - {Path.GetTempFileName()}"); // + TestData

            var expectedPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "ComplexReferenceFile.csv");
            var candidatePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "ComplexCandidateFile.csv");

            var outputPath = Path.Combine(Path.GetTempPath(), "Output");
            var comparisonDefinitionPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Definition.xml");

            try
            {
                ComparisonUtils.RunSingleComparison(comparisonDefinitionPath, expectedPath, candidatePath, "");
                Assert.That(!File.Exists(Path.Combine(outputPath, "ComparisonResults.BREAKS.csv")));

                ComparisonUtils.RunSingleComparison(comparisonDefinitionPath, expectedPath, candidatePath, outputPath);
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

        [TestCase("ComplexCandidateFile.csv")]
        [TestCase("ComplexReferenceFile.csv")]
        public void TestFolderComparison(string candidateFileName)
        {
            Console.WriteLine($"{Directory.GetCurrentDirectory()} - {Path.GetTempPath()} - {Path.GetTempFileName()}"); // + TestData

            var expectedPath = Path.Combine(Path.GetTempPath(), "ExpectedTest");
            if (!Directory.Exists(expectedPath))
            {
                Directory.CreateDirectory(expectedPath);
            }

            var candidatePath = Path.Combine(Path.GetTempPath(), "CandidateTest");
            if (!Directory.Exists(candidatePath))
            {
                Directory.CreateDirectory(candidatePath);
            }

            var outputPath = Path.Combine(Path.GetTempPath(), "Output");

            var expectedFile = Path.Combine(expectedPath, "ComplexReferenceFile.csv");
            var candidateFile = Path.Combine(candidatePath, candidateFileName);
            File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "TestData", "ComplexReferenceFile.csv"), expectedFile, true);
            File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "TestData", candidateFileName), candidateFile, true);

            var comparisonDefinitionPath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "MultipleDefinition.xml");
            try
            {
                // Run with no output path
                ComparisonUtils.RunDirectoryComparison(comparisonDefinitionPath, expectedPath, candidatePath, "");
                Assert.That(!File.Exists(Path.Combine(outputPath, "Reconciliation-Results-Test.BREAKS.csv")));

                ComparisonUtils.RunDirectoryComparison(comparisonDefinitionPath, expectedPath, candidatePath, outputPath);

                if (candidateFileName != "ComplexReferenceFile.csv")
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

                if (Directory.Exists(candidatePath))
                {
                    Directory.Delete(candidatePath, true);
                }

                if (Directory.Exists(outputPath))
                {
                    Directory.Delete(outputPath, true);
                }
            }
        }
    }
}
