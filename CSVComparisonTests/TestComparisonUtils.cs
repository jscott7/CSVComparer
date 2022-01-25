using CSVComparison;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CSVComparisonTests
{
    public class TestComparisonUtils
    {
        [Test]
        [Ignore("Test not yet fully implemented")]
        public void TestSingleFileComparison()
        {       
            Console.WriteLine($"{Directory.GetCurrentDirectory()} - {Path.GetTempPath()} - {Path.GetTempFileName()}"); // + TestData
          //  ComparisonUtils.RunSingleComparison(configurationFilePath, referenceFilePath, outputFilePath);
        }
    }
}
