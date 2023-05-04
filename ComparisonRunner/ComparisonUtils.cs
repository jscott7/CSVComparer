using CSVComparison;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace ComparisonRunner;

/// <summary>
/// Helpers for finding and loading CSV files for comparison
/// </summary>
public class ComparisonUtils
{
    private static bool AppendFile = false;

    /// <summary>
    /// Run comparison against all CSV files in a directory
    /// </summary>
    /// <param name="configurationFilePath">Path to MultipleComparisonDefinition</param>
    /// <param name="leftHandSideFilePath">Path to leftHandSide directory. This will be used as the source for files to match</param>
    /// <param name="rightHandSideFilePath">Path to rightHandSide directory</param>
    /// <param name="outputFilePath">Path to save output file for each comparison</param>
    /// <remarks>
    /// For each file in the leftHandSide directory, check to see if a configuration match exists then check to see if a matching rightHandSide file exists
    /// </remarks>
    public static void RunDirectoryComparison(string configurationFilePath, string leftHandSideFilePath, string rightHandSideFilePath, string outputFilePath)
    {
        var multiComparisonDefinition = DeserializeComparisonDefinition<MultipleComparisonDefinition>(configurationFilePath);

        // Now enumerate directory
        var leftHandSideDirectory = new DirectoryInfo(leftHandSideFilePath);

        foreach (var file in leftHandSideDirectory.GetFiles())
        {
            AppendFile = false;
            var stopwatch = Stopwatch.StartNew();
       
            Console.WriteLine($"Searching for comparison definition for {file}");

            // Get the comparisondefinition for the file, using the regex pattern
            var comparisonDefinitionForFileType = multiComparisonDefinition.FileComparisonDefinitions.Where(x => Regex.IsMatch(file.Name, x.FilePattern));

            if (comparisonDefinitionForFileType.Count() != 1)
            {
                Console.WriteLine($"No valid Comparison Definition found for {file.FullName}");
                continue;
            }

            var fileComparisonDefinition = comparisonDefinitionForFileType.First();

            Console.WriteLine($"Found Comparison Definition. ID = {fileComparisonDefinition.Key}");
            var csvComparer = new CSVComparer(fileComparisonDefinition.ComparisonDefinition);

            var rightHandSideFile = FindRightHandSideFile(rightHandSideFilePath, file, fileComparisonDefinition);
            if (string.IsNullOrEmpty(rightHandSideFile))
            {
                continue;
            }
           
            Console.WriteLine($"Comparing {file.FullName} with {rightHandSideFile}");
            var comparisonResult = csvComparer.CompareFiles(file.FullName, rightHandSideFile);
          
            stopwatch.Stop();
            var elapsedTime = stopwatch.ElapsedMilliseconds;
            HandleResult(comparisonResult, elapsedTime, fileComparisonDefinition, outputFilePath);
            Console.WriteLine($"Comparison took {stopwatch.ElapsedMilliseconds}ms\r\n");
        }

        Console.WriteLine("\nFinished.");
    }

    /// <summary>
    /// Run a single comparison between two CSV files
    /// </summary>
    /// <param name="configurationFilePath">Path to ComparisonDefinition</param>
    /// <param name="leftHandSideFilePath">Path to leftHandSide CSV file</param>
    /// <param name="rightHandSideFilePath">Path to rightHandSide CSV file</param>
    /// <param name="outputFilePath">Location of output file</param>
    public static void RunSingleComparison(string configurationFilePath, string leftHandSideFilePath, string rightHandSideFilePath, string outputFilePath)
    {
        var comparisonDefinition = DeserializeComparisonDefinition<ComparisonDefinition>(configurationFilePath);

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var csvComparer = new CSVComparer(comparisonDefinition);

        var comparisonResult = csvComparer.CompareFiles(new FileInfo(leftHandSideFilePath).FullName, new FileInfo(rightHandSideFilePath).FullName);
        stopwatch.Stop();

        Console.WriteLine($"LeftHandSide: {comparisonResult.LeftHandSideSource}");
        Console.WriteLine($"RightHandSide: {comparisonResult.RightHandSideSource}");

        if (string.IsNullOrEmpty(outputFilePath))
        {
            foreach (var breakResult in comparisonResult.BreakDetails)
            {
                Console.WriteLine(breakResult.ToString());
            }
        }
        else
        {
            if (!Directory.Exists(outputFilePath))
            {
                Console.WriteLine($"Creating: {outputFilePath}");
                Directory.CreateDirectory(outputFilePath);
            }

            var resultsFile = Path.Combine(outputFilePath, "ComparisonResults.csv");

            AppendFile = false;
            SaveResults(resultsFile, comparisonResult, comparisonDefinition, stopwatch.ElapsedMilliseconds);
        }

        Console.WriteLine($"Finished. Comparison took {stopwatch.ElapsedMilliseconds}ms");
    }

    private static void HandleResult(ComparisonResult comparisonResult, long elapsedTime, FileComparisonDefinition fileComparisonDefinition, string outputFilePath)
    {
        Console.WriteLine($"LeftHandSide: {comparisonResult.LeftHandSideSource}");
        Console.WriteLine($"RightHandSide: {comparisonResult.RightHandSideSource}");

        if (comparisonResult.BreakDetails.Count == 0)
        {
            Console.WriteLine("No differences found.");
        }
        else
        {
            Console.WriteLine($"{comparisonResult.BreakDetails.Count} differences found");
        }

        if (string.IsNullOrEmpty(outputFilePath))
        {
            foreach (var breakResult in comparisonResult.BreakDetails)
            {
                Console.WriteLine(breakResult.ToString());
            }
        }
        else
        {
            if (!Directory.Exists(outputFilePath))
            {
                Console.WriteLine($"Creating: {outputFilePath}");
                Directory.CreateDirectory(outputFilePath);
            }

            var resultsFile = Path.Combine(outputFilePath, $"Reconciliation-Results-{fileComparisonDefinition.Key}.csv");

            SaveResults(resultsFile, comparisonResult, fileComparisonDefinition.ComparisonDefinition, elapsedTime);
            if (!AppendFile)
            {
                AppendFile = true;
            }
        }
    }

    private static void SaveResults(string outputFile, ComparisonResult comparisonResult, ComparisonDefinition comparisonDefinition, long elapsedMillis)
    {
        if (comparisonResult.BreakDetails.Any())
        {
            outputFile = outputFile.Replace(".csv", ".BREAKS.csv");
        }

        Console.WriteLine($"Saving results to {outputFile}");

        using var sw = new StreamWriter(outputFile, AppendFile);
        var xmlSerializer = new XmlSerializer(typeof(ComparisonDefinition));

        // If we are checking multiple files with the same configuration we don't want to write it out more than once
        if (!AppendFile)
        {
            xmlSerializer.Serialize(sw, comparisonDefinition);
            sw.WriteLine();
        }

        sw.WriteLine();
        sw.WriteLine($"Date run,{comparisonResult.Date}");
        sw.WriteLine($"LHS,{comparisonResult.LeftHandSideSource}");
        sw.WriteLine($"RHS,{comparisonResult.RightHandSideSource}");
        sw.WriteLine($"Number of LHS rows,{comparisonResult.NumberOfLeftHandSideRows}");
        sw.WriteLine($"Number of RHS rows,{comparisonResult.NumberOfRightHandSideRows}");
        sw.WriteLine($"Duration,{elapsedMillis}ms");
        sw.WriteLine($"Number of breaks,{comparisonResult.BreakDetails.Count}");
        sw.WriteLine();

        if (comparisonResult.BreakDetails.Count > 0)
        {
            sw.WriteLine($"Break Type,Key ({comparisonResult.KeyDefinition}),Column Name,LHS Row,LHS Value,RHS Row,RHS Value");
            foreach (var breakResult in comparisonResult.BreakDetails)
            {
                sw.WriteLine($"{breakResult.BreakType},{breakResult.BreakKey},{breakResult.Column},{breakResult.LeftHandSideRow},{breakResult.LeftHandSideValue},{breakResult.RightHandSideRow},{breakResult.RightHandSideValue}");
            }
        }
    }

    /// <summary>
    /// Find a rightHandSide file that matches leftHandSide. Try exact file match first, then try filepattern match
    /// </summary>
    /// <param name="rightHandSideFilePath">Root path to rightHandSide files</param>
    /// <param name="leftHandSideFile">The leftHandSide file as base for search</param>
    /// <param name="fileComparisonDefinition">Source for any file regex pattern</param>
    /// <returns>Full path to rightHandSide file or empty string if not found</returns>
    private static string FindRightHandSideFile(string rightHandSideFilePath, FileInfo leftHandSideFile, FileComparisonDefinition fileComparisonDefinition)
    {
        var rightHandSideFile = "";

        if (File.Exists(Path.Combine(rightHandSideFilePath, leftHandSideFile.Name)))
        {
            rightHandSideFile = Path.Combine(rightHandSideFilePath, leftHandSideFile.Name);
        }
        else
        {
            var directoryInfo = new DirectoryInfo(rightHandSideFilePath);
            var regex = new Regex(fileComparisonDefinition.FilePattern);
            Console.WriteLine($"Exact file match for leftHandSide: '{leftHandSideFile.Name}' not found. Search using pattern: '{fileComparisonDefinition.FilePattern}'");
            var rightHandSidePaths = directoryInfo.GetFiles().Where(rightHandSideFile => regex.IsMatch(rightHandSideFile.Name));

            if (rightHandSidePaths.Count() == 1)
            {
                rightHandSideFile = rightHandSidePaths.First().FullName;
            }
            else
            {
                Console.WriteLine($"Unable to find a single matching file to compare with {leftHandSideFile.FullName}. Found {rightHandSidePaths.Count()}");
            }
        }

        if (!string.IsNullOrEmpty(rightHandSideFile))
        {
            var rightHandSideFileInfo = new FileInfo(rightHandSideFile);
            return rightHandSideFileInfo.FullName;
        }

         return rightHandSideFile;
    }
    
    /// <summary>
    /// Deserialize an XML file into a supported ComparisonDefinition type 
    /// </summary>
    /// <typeparam name="T">The ComparisonDefinition type</typeparam>
    /// <param name="configurationFilePath">Path to XML file</param>
    /// <returns>Instance of comparison definition type</returns>
    /// <exception cref="Exception">Unable to deserialize the file</exception>
    private static T DeserializeComparisonDefinition<T>(string configurationFilePath) where T : class
    {
        var xmlDocument = new XmlDocument();
        xmlDocument.Load(configurationFilePath);

        var xmlSerializer = new XmlSerializer(typeof(T));
        if (xmlDocument.DocumentElement == null)
        {
            throw new Exception($"Configuation {configurationFilePath} does not contain valid XML");
        }

        if (xmlSerializer.Deserialize(new XmlNodeReader(xmlDocument.DocumentElement)) is not T comparisonDefinition)
        {
            throw new Exception($"Unable to deserialize {configurationFilePath}");
        }

        return comparisonDefinition;
    }
}
