using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

namespace CSVComparison
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine($"Invalid number of arguments {args.Length}. Expecting 3");
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

            foreach (var breakResult in comparisonResult.BreakDetails)
            {
                Console.WriteLine(breakResult.ToString());
            }

            Console.WriteLine($"Finished. Comparison took {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
