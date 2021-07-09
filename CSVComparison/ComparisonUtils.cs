using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace CSVComparison
{
    public class ComparisonUtils
    {
        private static bool AppendFile = false;

        /// <summary>
        /// Run comparison against all files in a directory
        /// </summary>
        /// <param name="configurationFilePath">Path to MultipleComparisonDefinition</param>
        /// <param name="referenceFilePath">Path to reference directory. This will be used as the source for files to match</param>
        /// <param name="candidateFilePath">Path to candidate directory</param>
        /// <param name="outputFilePath">Path to save output file for each comparison</param>
        /// <remarks>
        /// For each file in the reference directory, check to see if a configuration match exists then check to see if a matching candidate file exists
        /// </remarks>
        public static void RunDirectoryComparison(string configurationFilePath, string referenceFilePath, string candidateFilePath, string outputFilePath)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(configurationFilePath);

            var xmlSerializer = new XmlSerializer(typeof(MultipleComparisonDefinition));
            var multiComparisonDefinition = (MultipleComparisonDefinition)xmlSerializer.Deserialize(new XmlNodeReader((XmlNode)xmlDocument.DocumentElement));

            // Now enumerate directory
            var referenceDirectory = new DirectoryInfo(referenceFilePath);

            foreach (var file in referenceDirectory.GetFiles())
            {
                AppendFile = false;
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                Console.WriteLine($"Searching for comparison definition for {file}");
                // Get the comparisondefinition for the file, using the pattern
                var comparisonDefinitionForFileType = multiComparisonDefinition.FileComparisonDefinitions.Where(x => Regex.IsMatch(file.Name, x.FilePattern));

                if (comparisonDefinitionForFileType.Count() != 1)
                {
                    Console.WriteLine($"No valid Comparison Definition found for {file.FullName}");
                    continue;
                }

                var fileComparisonDefinition = comparisonDefinitionForFileType.First();

                Console.WriteLine($"Found Comparison Definition. ID = {fileComparisonDefinition.Key}");
                var csvComparer = new CSVComparer(fileComparisonDefinition.ComparisonDefinition);

                // Search for candidate file. Try exact file match first, then try filepattern match
                ComparisonResult comparisonResult;
                if (File.Exists(Path.Combine(candidateFilePath, file.Name)))
                {
                    Console.WriteLine($"Comparing {file.FullName} with {Path.Combine(candidateFilePath, file.Name)}");
                    comparisonResult = csvComparer.CompareFiles(file.FullName, Path.Combine(candidateFilePath, file.Name));
                }
                else
                {
                    var directoryInfo = new DirectoryInfo(candidateFilePath);
                    var regex = new System.Text.RegularExpressions.Regex(fileComparisonDefinition.FilePattern);
                    Console.WriteLine($"Exact file match for reference: '{file.Name}' not found. Search using pattern: '{fileComparisonDefinition.FilePattern}'");
                    var candidatePaths = directoryInfo.GetFiles().Where(candidateFile => regex.IsMatch(candidateFile.Name));

                    if (candidatePaths.Count() != 1)
                    {
                        Console.WriteLine($"Unable to find a single matching file to compare with {file.FullName}. Found {candidatePaths.Count()}");
                        continue;
                    }

                    Console.WriteLine($"Comparing {file.FullName} with {candidatePaths.First().FullName}");

                    comparisonResult = csvComparer.CompareFiles(file.FullName, candidatePaths.First().FullName);
                }

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
        /// <param name="referenceFilePath">Path to reference CSV file</param>
        /// <param name="candidateFilePath">Path to candidate CSV file</param>
        /// <param name="outputFilePath">Location of output file</param>
        public static void RunSingleComparison(string configurationFilePath, string referenceFilePath, string candidateFilePath, string outputFilePath)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(configurationFilePath);

            var xmlSerializer = new XmlSerializer(typeof(ComparisonDefinition));
            var comparisonDefinition = (ComparisonDefinition)xmlSerializer.Deserialize(new XmlNodeReader((XmlNode)xmlDocument.DocumentElement));

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var csvComparer = new CSVComparer(comparisonDefinition);
            var comparisonResult = csvComparer.CompareFiles(referenceFilePath, candidateFilePath);
            stopwatch.Stop();

            Console.WriteLine($"Reference: {comparisonResult.ReferenceSource}");
            Console.WriteLine($"Candidate: {comparisonResult.CandidateSource}");

            if (string.IsNullOrEmpty(outputFilePath))
            {
                foreach (var breakResult in comparisonResult.BreakDetails)
                {
                    Console.WriteLine(breakResult.ToString());
                }
            }
            else
            {
                var resultsFile = Path.Combine(outputFilePath, "ComparisonResults.csv");
                Console.WriteLine($"Saving results to {resultsFile}");
                AppendFile = false;
                SaveResults(resultsFile, comparisonResult, comparisonDefinition, stopwatch.ElapsedMilliseconds);
            }

            Console.WriteLine($"Finished. Comparison took {stopwatch.ElapsedMilliseconds}ms");
        }

        private static void HandleResult(ComparisonResult comparisonResult, long elapsedTime, FileComparisonDefinition fileComparisonDefinition, string outputFilePath)
        {
            Console.WriteLine($"Reference: {comparisonResult.ReferenceSource}");
            Console.WriteLine($"Candidate: {comparisonResult.CandidateSource}");

            if (comparisonResult.BreakDetails.Count() == 0)
            {
                Console.WriteLine("No differences found.");
            }
            else
            {
                Console.WriteLine($"{comparisonResult.BreakDetails.Count()} differences found");
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

            using (var sw = new StreamWriter(outputFile, AppendFile))
            {
                var xmlSerializer = new XmlSerializer(typeof(ComparisonDefinition));

                // If we are checking multiple files with the same configuration we don't want to write it out more than once
                if (!AppendFile)
                {
                    xmlSerializer.Serialize(sw, comparisonDefinition);
                    sw.WriteLine();
                }

                sw.WriteLine();
                sw.WriteLine($"Date run,{comparisonResult.Date}");
                sw.WriteLine($"Reference,{comparisonResult.ReferenceSource}");
                sw.WriteLine($"Candidate,{comparisonResult.CandidateSource}");
                sw.WriteLine($"Number of Reference rows,{comparisonResult.NumberOfReferenceRows}");
                sw.WriteLine($"Number of Candidate rows,{comparisonResult.NumberOfCandidateRows}");
                sw.WriteLine($"Duration,{elapsedMillis}ms");
                sw.WriteLine($"Number of breaks,{comparisonResult.BreakDetails.Count()}");
                sw.WriteLine();

                if (comparisonResult.BreakDetails.Count() > 0)
                {
                    sw.WriteLine("Break Type,Key,Column Name,Reference Row, Reference Value, Candidate Row, Candidate Value");
                    foreach (var breakResult in comparisonResult.BreakDetails)
                    {
                        sw.WriteLine($"{breakResult.BreakType},{breakResult.BreakKey},{breakResult.Column},{breakResult.ReferenceRow},{breakResult.ReferenceValue},{breakResult.CandidateRow},{breakResult.CandidateValue}");
                    }
                }
            }
        }
    }
}
