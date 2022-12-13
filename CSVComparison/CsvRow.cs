namespace CSVComparison
{
    /// <summary>
    /// A representation of a single CSV Row
    /// </summary>
    public class CsvRow
    {
        /// <summary>
        /// The key defining uniqueness for this row
        /// </summary>
        public string Key;

        public string[] Columns;

        /// <summary>
        /// Index of this row in the CSV File
        /// </summary>
        public int RowIndex;
    }
}
