using System;
using System.Collections.Generic;
using System.Text;

namespace CSVComparison
{
    public class RowHelper
    {
        public static List<string> SplitRowWithQuotes(ReadOnlySpan<char> line, string delimiter)
        {
            var delimiterIndex = line.IndexOf(delimiter);

            return null;
        }

            /// <summary>
            /// Split a string that can have delimiters embedded in quotes, for example: A,B,"C,D",E
            /// </summary>
            /// <param name="line">The full CSV line</param>
            /// <returns>List of each CSV Column</returns>
         public static List<string> SplitRowWithQuotes(string line, string delimiter)
        {
            var startingQuoteIndex = line.IndexOf("\"");
            var columnValues = new List<string>();

            int quoteSearchIndex = 0;
            int endQuoteIndex;
            int currentIndex;
            while ((currentIndex = line.IndexOf(delimiter, quoteSearchIndex)) > 0)
            {
                int startIndex = quoteSearchIndex;

                if (startingQuoteIndex > -1 && startingQuoteIndex >= quoteSearchIndex && startingQuoteIndex < currentIndex)
                {
                    // Get the end quote
                    endQuoteIndex = GetEndQuoteIndex(line, startingQuoteIndex);
                    if (endQuoteIndex == -1 || endQuoteIndex == line.Length - 1)
                    {
                        currentIndex = line.Length;
                    }
                    else
                    {
                        currentIndex = endQuoteIndex + 1;
                        startingQuoteIndex = line.IndexOf("\"", currentIndex + 1);
                    }
                }

                columnValues.Add(line.Substring(startIndex, currentIndex - startIndex));
                if (currentIndex < line.Length)
                {
                    quoteSearchIndex = currentIndex + delimiter.Length;
                }
                else
                {
                    quoteSearchIndex = currentIndex;
                }
            }

            if (quoteSearchIndex < line.Length)
            {
                columnValues.Add(line.Substring(quoteSearchIndex, line.Length - quoteSearchIndex));
            }

            // If the last character is a delimiter we will use the convention that this indicates there is one more column 
            if (line.EndsWith(delimiter))
            {
                columnValues.Add("");
            }

            return columnValues;
        }

        /// <summary>
        /// Get the location of the end matching quote
        /// </summary>
        /// <param name="line">The full CSV line</param>
        /// <param name="startingQuoteIndex">Index of the opening quite</param>
        /// <returns>The index of the end quote matching the opening quote</returns>
        /// <remarks>As per CSV RFC-4180 pairs of quotes are ignored, ""
        /// For example A,B,"A ""Test"" Value
        /// Will return "A ""Test"" Value" as a single field</remarks>
        private static int GetEndQuoteIndex(string line, int startingQuoteIndex)
        {
            bool terminated = false;
            int queryIndex = startingQuoteIndex;
            while (!terminated)
            {
                int nextQuoteIndex = line.IndexOf("\"", queryIndex + 1);

                if (nextQuoteIndex + 1 == line.Length || (nextQuoteIndex + 1 < line.Length && line[nextQuoteIndex + 1] != '\"'))
                {
                    return nextQuoteIndex;
                }
                else if (line[nextQuoteIndex + 1] == '\"')
                {
                    //This is a double quote
                    queryIndex = nextQuoteIndex + 1;
                }
                else if (nextQuoteIndex == -1)
                {
                    terminated = true;
                }
                else
                {
                    throw new Exception($"Unable to determine quotes for {line}");
                }
            }

            return -1;
        }
    }
}
