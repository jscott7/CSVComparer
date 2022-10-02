using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using CSVComparison;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class ComparerBenchmarks
    {
        [Benchmark(Baseline=true)]
        public int CompareIdentical()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ReferenceTest.csv");
            var candidateDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ReferenceTest.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("COL A");
            comparisonDefinition.HeaderRowIndex = 0;
            comparisonDefinition.ToleranceValue = 0.1;
            comparisonDefinition.ToleranceType = ToleranceType.Relative;

            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, candidateDataFile);
            return comparisonResult.BreakDetails.Count;
        }

        [Benchmark]
        public int CompareDifferent()
        {
            var referenceDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "ReferenceTest.csv");
            var candidateDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "CandidateTest.csv");

            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            comparisonDefinition.KeyColumns.Add("COL A");
            comparisonDefinition.HeaderRowIndex = 0;
            comparisonDefinition.ToleranceValue = 0.1;
            comparisonDefinition.ToleranceType = ToleranceType.Relative;

            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceDataFile, candidateDataFile);
            return comparisonResult.BreakDetails.Count;
        }
    }
}
