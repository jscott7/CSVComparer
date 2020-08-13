namespace CSVComparison
{
    public class FileComparisonDefinition
    {
        public string Key;

        /// <summary>
        /// Pattern for files that will use this ComparisonDefinition
        /// </summary>
        public string FilePattern;

        /// <summary>
        /// The Comparison
        /// </summary>
        public ComparisonDefinition ComparisonDefinition;
    }
}
