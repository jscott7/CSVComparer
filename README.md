# CSVComparison
A tool to compare 2 CSV files. Results of the comparison are saved to an output file or can be interrogated via an API.

[![NuGet Version](https://img.shields.io/nuget/v/CSVComparer.svg)](https://www.nuget.org/packages?q=CSVComparer)
[![Build Status](https://dev.azure.com/jonathanscott80/CSVComparer/_apis/build/status%2Fjscott7.CSVComparer?branchName=main)](https://dev.azure.com/jonathanscott80/CSVComparer/_build/latest?definitionId=2&branchName=main)
[![CodeQL](https://github.com/jscott7/CSVComparer/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/jscott7/CSVComparer/actions/workflows/codeql-analysis.yml)

[See here for more information and a list of updates](https://github.com/jscott7/CSVComparer/wiki)

## Some terminology

* **Left Hand Side file** The first CSV file for the comparison.
* **Right Hand Side file** The second CSV file for the comparison.
* **Key** Unique definition of a single CSV Row. This can be made from one or more Columns
* **Break** A single difference between the files. There may be multiple breaks.
* **Orphan** A row in the reference file but not in the candidate file. Or vice-versa.
* **Value Break** A difference in a column value between a row with matching key on Reference and Candidate file 

## Features
* Any number of columns in the CSV can be defined as keys
* CSV Files do not require any pre-sorting
* Supports any delimiter, including string delimiters, e.g. "=="
* Columns can be excluded (for example timestamps)
* Rows can be excluded based on Key pattern matching
* Orphans can be excluded based on pattern matching
* Additional rows at the start and end of file can be excluded, for example rows with footer information
* Supports Numeric tolerance, relative and absolute
* Double Quotes in a row supported e.g.  A,B,"C,D are one column",E

## How to use
The CSVComparison is available either by building from this project or from NuGet. In both cases an xml configuration is used to define how to compare two CSV files. The comparer is then run against LHS and RHS files using this configuration.

### Configuration
The configuration is used to define how to treat the CSV files:

```html
<?xml version="1.0" encoding="utf-8"?>
<ComparisonDefinition xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Delimiter>,</Delimiter>
  <KeyColumns>
    <Column>COL A</Column>
    <Column>COL B</Column>
  </KeyColumns>
  <ExcludedColumns>
    <Column>COL D</Column>
    <Column>COL E</Column>
  </ExcludedColumns>
  <IgnoreInvalidRows>true</IgnoreInvalidRows>
  <HeaderRowIndex>0</HeaderRowIndex>
  <ToleranceType>Relative</ToleranceType>
  <ToleranceValue>0.1</ToleranceValue>
  <OrphanExclusions>
    <ExclusionPattern>RegexPattern</ExclusionPattern>
  </OrphanExclusions>
  <KeyExclusions>
    <ExclusionPattern>RegexPattern</ExclusionPattern>
  </KeyExclusions>
</ComparisonDefinition>
```

**Delimiter**  Allows other separaters, i.e. pipe '|' to be used

**KeyColumns** Lists the columns required to obtain a unique key for each row

**ExcludedColumns** List the columns to be excluded from the comparison

**IgnoreInvalidRows** If a row doesn't have the same number of columns (it may be a descriptive footer for example) do not include in the comparison

**HeaderRowIndex** Set the row for header columns, if some non csv data occurs at the start of the file

**ToleranceType** How to compare numeric values

**ToleranceValue** The tolerance to use for numeric values

**OrphanExclusions** A list of Regex Patterns used to exclude orphans whose key matches the pattern

**KeyExclusions** A list of Regex Patterns used to exclude Value breaks whose key matches the pattern
 
 
### Comparison Runner
The solution includes a ComparisonRunner project which builds an exe that can be used with the following arguments:

*"Path to left hand side csv file" "Path to right hand side reference file" "Path to configuration file" "Optional Path to directory to save output*

If no output file is specified the console will list the breaks between the files

`Key:C, LeftHandSide Row:2, Value:2.5 != RightHandSide Row:2, Value:2.61`

The output file will list the configuration used, the input files, time taken to run the comparison and a tabular view of the differences

```html
<?xml version="1.0" encoding="utf-8"?>
<ComparisonDefinition xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Delimiter>,</Delimiter>
  <KeyColumns>
    <Column>COL A</Column>
  </KeyColumns>
  <HeaderRowIndex>0</HeaderRowIndex>
  <ToleranceValue>0.1</ToleranceValue>
  <IgnoreInvalidRows>false</IgnoreInvalidRows>
  <ToleranceType>Relative</ToleranceType>
  <ExcludedColumns />
</ComparisonDefinition>

Date run: 11/01/2021 18:32:48
LeftHandSide: C:\temp\LeftHandSideDirectory\Test.csv
RightHandSide: C:\temp\RightHandSideDirectory\Test.csv
Number of LeftHandSide rows: 100001
Number of RightHandSide rows: 100001
Comparison took 906ms
Number of breaks 5

Break Type,Key - COL A,Column Name,LHS Row,LHS Value,RHS Row,RHS Value
ValueMismatch,1,COL B,2,A,2,"A,X"
ValueMismatch,7,COL D,8,32.1,8,42.1
ValueMismatch,77,COL B,78,B,78,A
RowInRHS_NotInLHS,100000,,-1,,100000,
RowInLHS_NotInRHS,99,,100,,-1,
```

In tabular form (open in a spreadsheet)

|Break Type|Key - COL A|Column Name|LHS Row|LHS Value|RHS Row|RHS Value|
|----------|-----------|-----------|-------|---------|-------|---------|
|ValueMismatch|1|COL B|2|A|2|"A,X"|
|ValueMismatch|7|COL D|8|32.1|8|42.1|
|ValueMismatch|77|COL B|78|B|78|A|
|RowInRHS_NotInLHS|100000||-1||100000||
|RowInLHS_NotInRHS|99||100||-1|


### Directory comparison

If the LHS and RHS paths are directories you can compare multiple files. If the files have different structures a single global configuration can
be created that can define all comparisons.

The FilePattern element is a Regex pattern that is used to determine the configuration.

```html
<MultipleComparisonDefinition>
  <FileComparisonDefinitions>
    <Comparison>
      <Key>Test</Key>
      <FilePattern>Test</FilePattern>
      <ComparisonDefinition xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
        <Delimiter>,</Delimiter>
        <KeyColumns>
          <Column>COL A</Column>
        </KeyColumns>
        <HeaderRowIndex>0</HeaderRowIndex>
        <ToleranceValue>0.1</ToleranceValue>
        <ToleranceType>Relative</ToleranceType>
      </ComparisonDefinition>
    </Comparison>
    <Comparison>
      <Key>Test2</Key>
      <FilePattern>File</FilePattern>
      <ComparisonDefinition xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
        <Delimiter>,</Delimiter>
        <KeyColumns>
          <Column>COL 1</Column>
          <Column>COL 2</Column>
        </KeyColumns>
        <HeaderRowIndex>0</HeaderRowIndex>
        <ToleranceValue>0.1</ToleranceValue>
        <ToleranceType>Relative</ToleranceType>
      </ComparisonDefinition>
    </Comparison>
  </FileComparisonDefinitions>
</MultipleComparisonDefinition>
```

The comparison will check each file in the reference directory. If a Comparison is found using the file pattern an exact file match is first attempted in the 
candidate directory. If an exact match does not happen then a search for a single file that matches the pattern will be performed. 

The comparison will be performed only if:
* A comparison definition is found
* Exactly one candidate file is found, either by exact match or file pattern match

### TestGenerator Tool
The TestGenerator project very simply builds two CSV files of user-defined lengh with random breaks added.

## NuGet API

To run from your own C# code, first install the CSVComparer packgage from NuGet.org into your project

The following code will compare two CSV files given their paths. The files both contain columns named ABC and DEF which together define uniqueness for each row.

```csharp
 # You can either deserialize the comparison definition xml or create your own in code
 var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
 comparisonDefinition.KeyColumns.Add("ABC");
 comparisonDefinition.KeyColumns.Add("DEF");

 var csvComparer = new CSVComparer(comparisonDefinition);
 var comparisonResult = csvComparer.CompareFiles(lhsCsvFilePath, rhsCsvFilePath);
 Console.WriteLine($"Time started {comparisonResult.Date}. #LHS Rows {comparisonResult.NumberOfLeftHandSideRows}. #RHS Rows {comparisonResult.NumberOfRightHandSideRows}");
            
 # Add code to interrogate the comparison result.
 foreach(var breakDetail in comparisonResult.BreakDetails)
 {
      Console.WriteLine($"{breakDetail.BreakType} - {breakDetail.BreakDescription}");
 }
```

The comparisonResult output contains high level summary information and a list of all differences or breaks found between the two inputs.

Each break detail contains the following properties describing the difference in detail:
 
```csharp
    /// <summary>
    /// The type of Break
    /// </summary>
    public BreakType BreakType;

    /// <summary>
    /// A single line description of the break
    /// </summary>
    public string BreakDescription;

    /// <summary>
    /// The key of the row in the CSV file
    /// </summary>
    public string BreakKey;

    /// <summary>
    /// The index of the row in the leftHandSide CSV file
    /// </summary>
    public int LeftHandSideRow;

    /// <summary>
    /// The index of the row in the rightHandSide CSV file
    /// </summary>
    public int RightHandSideRow;

    /// <summary>
    /// The name of the column of the mismatching data. Will be blank if the row is an orphan
    /// </summary>
    public string Column;

    /// <summary>
    /// The value of mismatching data. Will be blank if the row is an orphan
    /// </summary>
    public string LeftHandSideValue;

    /// <summary>
    /// The value of mismatching data. Will be blank if the row is an orphan
    /// </summary>
    public string RightHandSideValue;
```

## Benchmarks
BenchmarkDotNet is used to obtain the information below, stats are obtained by running a comparison of two files containing 1000 rows, generated using the TestDataGenerator tool.

From a Release Build, open a command prompt in CSVComparer\Benchmark\bin\Release\net6.0 and run Benchmark.exe

``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22621.1413/22H2/2022Update/SunValley2)
11th Gen Intel Core i7-11800H 2.30GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=7.0.202
  [Host]     : .NET 6.0.15 (6.0.1523.11507), X64 RyuJIT AVX2
  DefaultJob : .NET 6.0.15 (6.0.1523.11507), X64 RyuJIT AVX2


```
|                       Method |            Mean |         Error |        StdDev | Ratio | RatioSD |    Gen0 |   Gen1 | Allocated | Alloc Ratio |
|----------------------------- |----------------:|--------------:|--------------:|------:|--------:|--------:|-------:|----------:|------------:|
|             CompareIdentical | 1,269,694.12 ns | 22,525.557 ns | 25,037.097 ns | 1.000 |    0.00 | 66.4063 | 5.8594 |  830445 B |       1.000 |
|             CompareDifferent | 1,288,638.57 ns | 25,125.239 ns | 37,606.275 ns | 1.019 |    0.05 | 66.4063 | 7.8125 |  837210 B |       1.008 |
|                  StringSplit |        64.24 ns |      1.257 ns |      1.544 ns | 0.000 |    0.00 |  0.0178 |      - |     224 B |       0.000 |
| StringSplitWithQuotesControl |       139.90 ns |      2.598 ns |      2.431 ns | 0.000 |    0.00 |  0.0267 |      - |     336 B |       0.000 |
|        StringSplitWithQuotes |       145.07 ns |      2.617 ns |      2.448 ns | 0.000 |    0.00 |  0.0267 |      - |     336 B |       0.000 |


Legends
*  Mean        : Arithmetic mean of all measurements
*  Error       : Half of 99.9% confidence interval
*  StdDev      : Standard deviation of all measurements
*  Ratio       : Mean of the ratio distribution ([Current]/[Baseline])
*  RatioSD     : Standard deviation of the ratio distribution ([Current]/[Baseline])
*  Gen0        : GC Generation 0 collects per 1000 operations
*  Gen1        : GC Generation 1 collects per 1000 operations
*  Allocated   : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
*  Alloc Ratio : Allocated memory ratio distribution ([Current]/[Baseline])


Before changing to use ReadOnlySpan<char> for StringSplitWithQuotes, the benchmark was:

|                       Method |            Mean |         Error |        StdDev | Ratio | RatioSD |    Gen0 |   Gen1 | Allocated | Alloc Ratio |
|----------------------------- |----------------:|--------------:|--------------:|------:|--------:|--------:|-------:|----------:|------------:|
|        StringSplitWithQuotes |       352.94 ns |      6.950 ns |      6.501 ns | 0.000 |    0.00 |  0.0267 |      - |     336 B |       0.000 |