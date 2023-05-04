using ComparisonRunner;

if (args.Length < 3)
{
    Console.WriteLine($"Invalid number of arguments {args.Length}. Expecting at least 3");
    Console.WriteLine("Arguments used:");
    foreach(string arg in args)
    {
        Console.WriteLine(arg);
    }

    Console.WriteLine("Usage: CsVComparison LeftHandSideFilePath RightHandSideFilePath ConfigurationFilePath [PathToOutputFile]");
    return;
}

var leftHandSideFilePath = args[0];
var rightHandSideFilePath = args[1]; 
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
    
if (Directory.Exists(leftHandSideFilePath))
{
    // This is a directory
    ComparisonUtils.RunDirectoryComparison(configurationFilePath, leftHandSideFilePath, rightHandSideFilePath, outputFilePath);
}
else
{
    // Default to single file comparison
    ComparisonUtils.RunSingleComparison(configurationFilePath, leftHandSideFilePath, rightHandSideFilePath, outputFilePath);
}
