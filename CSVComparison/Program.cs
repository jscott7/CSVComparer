using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace CSVComparison
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length <= 3)
            {
                Console.WriteLine($"Invalid number of arguments {args.Length}. Expecting at least 3");
                Console.WriteLine("Arguments used:");
                foreach(string arg in args)
                {
                    Console.WriteLine(arg);
                }
                return;
            }

            var referenceFilePath = args[0];
            var targetFilePatch = args[1];
            var configurationFilePath = args[2];

            string outputFile = "";
            if (args.Length > 3)
            {
                outputFile = args[3];
            }

            var xmlDocument = new XmlDocument();
            xmlDocument.Load(configurationFilePath);

            var xmlSerializer = new XmlSerializer(typeof(ComparisonDefinition));
            var comparisonDefinition = (ComparisonDefinition)xmlSerializer.Deserialize(new XmlNodeReader((XmlNode)xmlDocument.DocumentElement));

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var csvComparer = new CSVComparer();
            var comparisonResult = csvComparer.CompareFiles(referenceFilePath, targetFilePatch, comparisonDefinition);
            stopwatch.Stop();

            Console.WriteLine($"Reference: {comparisonResult.ReferenceSource}");
            Console.WriteLine($"Target: {comparisonResult.CandidateSource}");

            if (string.IsNullOrEmpty(outputFile))
            {
                foreach (var breakResult in comparisonResult.BreakDetails)
                {
                    Console.WriteLine(breakResult.ToString());
                }
            }
            else
            {
                Console.WriteLine($"Saving results to {outputFile}");
                SaveResults(outputFile, comparisonResult, comparisonDefinition, stopwatch.ElapsedMilliseconds);
            }

            Console.WriteLine($"Finished. Comparison took {stopwatch.ElapsedMilliseconds}ms");
        }

        private static void SaveResults(string outputFile, ComparisonResult comparisonResult, ComparisonDefinition comparisonDefinition, long elapsedMillis)
        {
            using (var sw = new StreamWriter(outputFile))
            {
                var xmlSerializer = new XmlSerializer(typeof(ComparisonDefinition));
                xmlSerializer.Serialize(sw, comparisonDefinition);
                sw.WriteLine();
                sw.WriteLine($"Reference: {comparisonResult.ReferenceSource}");
                sw.WriteLine($"Target: {comparisonResult.CandidateSource}");
                sw.WriteLine($"Comparison took {elapsedMillis}ms");

                sw.WriteLine("Break Type,Key,Reference Row, Reference Value, Candidate Row, Candidate Value");
                foreach(var breakResult in comparisonResult.BreakDetails)
                {
                    sw.WriteLine($"{breakResult.BreakType},{breakResult.BreakKey},{breakResult.ReferenceRow},{breakResult.ReferenceValue},{breakResult.CandidateRow},{breakResult.CandidateValue}");
                }
            }
        }
    }
}