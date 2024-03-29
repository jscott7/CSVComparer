﻿using BenchmarkDotNet.Attributes;
using CSVComparison;

namespace Benchmark;

[MemoryDiagnoser]
public class ComparerBenchmarks
{
    [Benchmark(Baseline=true)]
    public int CompareIdentical()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "LeftHandSideTest.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "LeftHandSideTest.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("COL A");
        comparisonDefinition.HeaderRowIndex = 0;
        comparisonDefinition.ToleranceValue = 0.1;
        comparisonDefinition.ToleranceType = ToleranceType.Relative;

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);
        return comparisonResult.BreakDetails.Count;
    }

    [Benchmark]
    public int CompareDifferent()
    {
        var leftHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "LeftHandSideTest.csv");
        var rightHandSideDataFile = Path.Combine(AppContext.BaseDirectory, "TestData", "RightHandSideTest.csv");

        var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
        comparisonDefinition.KeyColumns.Add("COL A");
        comparisonDefinition.HeaderRowIndex = 0;
        comparisonDefinition.ToleranceValue = 0.1;
        comparisonDefinition.ToleranceType = ToleranceType.Relative;

        var csvComparer = new CSVComparer(comparisonDefinition);
        var comparisonResult = csvComparer.CompareFiles(leftHandSideDataFile, rightHandSideDataFile);
        return comparisonResult.BreakDetails.Count;
    }

    private const string RowNoQuotes = "Val1,Val2,Val3,Val4,Val5";
    private const string RowQuotes = "Val1,Val2,\"Val3,Val3B\",Val4,Val5";

    [Benchmark]
    public void StringSplit()
    {
        var split = RowNoQuotes.Split(',');
    }

    [Benchmark]
    public void StringSplitWithQuotesControl()
    {
        var split = RowHelper.SplitRowWithQuotes(RowNoQuotes, ",");
        if (split.Count != 5)
        {
            throw new Exception("ARRGH");
        }
    }

    [Benchmark]
    public void StringSplitWithQuotes()
    {
        var split = RowHelper.SplitRowWithQuotes(RowNoQuotes, ",");
        if (split.Count != 5)
        { 
            throw new Exception("ARRGH");
        }
    }

}
