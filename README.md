# CSVComparison

This is a simple project to compare 2 CSV files and report differences

## How to use

Run the CSVComparison executable with the following arguments

*"Path to reference csv file" "Path to candidate reference file" "Path to configuration file" "Optional Path to directory to save output*

If no output file is specficied the console will list the differences between the files

`Key:C, Reference Row:2, Value:2.5 != Candidate Row:2, Value:2.61`

The output file will list the configuration used, the input files, time taken to run the comparison and a tabular view of the differences
See the ExampleOutputFile.csv

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
</ComparisonDefinition>
```

**Delimiter**  Allows other separaters, i.e. pipe '|' to be used

**KeyColumns** Lists the columns required to obtain a unique key for each row

**ExcludedColumns** List the columns to be excluded from the comparison

**IgnoreInvalidRows** If a row doesn't have the same number of columns (it may be a descriptive footer for example) do not include in the comparison

**HeaderRowIndex** Set the row for header columns, if some non csv data occurs at the start of the file

**ToleranceType** How to compare numeric values

**ToleranceValue** The tolerance to use for numeric values

## Directory comparison

If the Reference and Candidate paths are directories you can compare multiple files. If the files have different structures a configuration can
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

 # Add code to interrogate the comparison result
```