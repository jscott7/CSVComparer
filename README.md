[![Build Status](https://dev.azure.com/jonathanscott80/CSVComparer/_apis/build/status%2Fjscott7.CSVComparer?branchName=main)](https://dev.azure.com/jonathanscott80/CSVComparer/_build/latest?definitionId=2&branchName=main)
[![CodeQL](https://github.com/jscott7/CSVComparer/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/jscott7/CSVComparer/actions/workflows/codeql-analysis.yml)

# CSVComparison

A tool to compare 2 CSV files. Results of the comparison are saved to an output file or can be interrogated via an API.
[See here for more information and a list of updates](https://github.com/jscott7/CSVComparer/wiki)

## Some terminology

* **Left Hand Side file** The first CSV file for the comparison.
* **Right Hand Side file** The second CSV file for the comparison.
* **Key** Unique definition of a single CSV Row. This can be made from one or more Columns
* **Break** A single difference between the files. There may be multiple breaks.
* **Orphan** A row in the reference file but not in the candidate file. Or vice-versa.
* **Value Break** A difference in a column value between a row with matching key on Reference and Candidate file 

## How to use

Run the CSVComparison executable with the following arguments

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

##  Configuration
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
 
## Directory comparison

If the LHS and RHS paths are directories you can compare multiple files. If the files have different structures a configuration can
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

## API

To run from your own C# code:

```csharp
 # You can either deserialize the comparison definition xml or create your own in code
 var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
 comparisonDefinition.KeyColumns.Add("ABC");
 comparisonDefinition.KeyColumns.Add("DEF");

 var csvComparer = new CSVComparer(comparisonDefinition);
 var comparisonResult = csvComparer.CompareFiles(referenceDataFilePath, targetDataFilePath);

 # Add code to interrogate the comparison result.
 foreach(var breakDetail in comparisonResult.BreakDetails)
 {
      Console.WriteLine($"{breakDetail.BreakType} - {breakDetail.BreakDescription}");
 }
```

## BenchmarkDotNet
Running a comparison of two files (containing 1000) rows.
Generated using the TestDataGenerator tool

From a Release Build, open a command prompt in CSVComparer\Benchmark\bin\Release\net6.0 and run Benchmark.exe

``` ini

BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22621.1413)
11th Gen Intel Core i7-11800H 2.30GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK=7.0.202
  [Host]     : .NET 6.0.15 (6.0.1523.11507), X64 RyuJIT AVX2
  DefaultJob : .NET 6.0.15 (6.0.1523.11507), X64 RyuJIT AVX2

```

|           Method |     Mean |     Error |    StdDev | Ratio | RatioSD |    Gen0 |   Gen1 | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| CompareIdentical | 1.431 ms | 0.0136 ms | 0.0127 ms |  1.00 |    0.00 | 54.6875 | 3.9063 | 667.77 KB |        1.00 |
| CompareDifferent | 1.407 ms | 0.0194 ms | 0.0182 ms |  0.98 |    0.02 | 54.6875 | 5.8594 | 673.76 KB |        1.01 |

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
