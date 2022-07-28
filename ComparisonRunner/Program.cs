
using ComparisonRunner;

if (args.Length < 3)
{
    Console.WriteLine($"Invalid number of arguments {args.Length}. Expecting at least 3");
    Console.WriteLine("Arguments used:");
    foreach(string arg in args)
    {
        Console.WriteLine(arg);
    }

    Console.WriteLine("Usage: CsVComparison ReferenceFilePath CandidateFilePath ConfigurationFilePath [PathToOutputFile]");
    return;
}

var referenceFilePath = args[0];
var candidateFilePath = args[1]; 
var configurationFilePath = args[2];

string outputFilePath = "";
if (args.Length > 3)
{
    outputFilePath = args[3];
    if (!Directory.Exists(outputFilePath))
    {
        Console.WriteLine($"Creating {outputFilePath} to save results");
        Directory.CreateDirectory(outputFilePath);
    }
}
    
if (Directory.Exists(referenceFilePath))
{
    // This is a directory
    ComparisonUtils.RunDirectoryComparison(configurationFilePath, referenceFilePath, candidateFilePath, outputFilePath);
}
else
{
    // Default to single file comparison
    ComparisonUtils.RunSingleComparison(configurationFilePath, referenceFilePath, candidateFilePath, outputFilePath);
}
