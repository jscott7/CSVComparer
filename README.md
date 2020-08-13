# CSVComparison

This is a simple project to compare 2 CSV files and report differences

## How to use

Run the CSVComparison executable with the following arguments

*"Path to reference csv file" "Path to candidate reference file" "Path to configuration file" "Optional Path to output file*

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
  <IgnoreInvalidRows>true</IgnoreInvalidRows>
  <HeaderRowIndex>0</HeaderRowIndex>
  <ToleranceType>Relative</ToleranceType>
  <ToleranceValue>0.1</ToleranceValue>
</ComparisonDefinition>
```

**Delimiter**  Allows other separaters, i.e. pipe '|' to be used

**KeyColumns** Lists the columns required to obtain a unique key for each row

**IgnoreInvalidRows** If a row doesn't have the same number of columns (it may be a descriptive footer for example) do not include in the comparison

**HeaderRowIndex** Set the row for header columns, if some non csv data occurs at the start of the file

**ToleranceType** How to compare numeric values

**ToleranceValue** The tolerance to use for numeric values

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