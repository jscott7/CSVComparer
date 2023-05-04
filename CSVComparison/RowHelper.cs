using System;
using System.Collections.Generic;
using System.Text;

namespace CSVComparison;

public class RowHelper
{
    public static List<string> SplitRowWithQuotes(ReadOnlySpan<char> line, string delimiter)
    {
        var columnValues = new List<string>();  
        ReadOnlySpan<char> remaining = line;

        while (remaining.Length > 0)
        {
            var nextDelimiterIndex = remaining.IndexOf(delimiter);
            var nextQuoteIndex = remaining.IndexOf("\"");

            if (nextDelimiterIndex == -1)
            {
                // Add the last characters on the row
                columnValues.Add(remaining[0..remaining.Length].ToString());
                break;
            }
            else if (nextQuoteIndex == -1 || nextDelimiterIndex < nextQuoteIndex)
            {
                columnValues.Add(remaining[0..nextDelimiterIndex].ToString());
                if (remaining.Length == 1) {
                    columnValues.Add("");
                }

                remaining = remaining[(nextDelimiterIndex + delimiter.Length)..remaining.Length];
            }
            else if (nextQuoteIndex > -1)
            {
                bool isInQuoteBlock = true;
                var quote = new StringBuilder();

                while (isInQuoteBlock)
                {
                    remaining = remaining[(nextQuoteIndex + 1)..remaining.Length];
                    var quoteIndex = remaining.IndexOf("\"");
                    if (quoteIndex == -1)
                    {
                        // There is no closing quote. This is invalid CSV so save out all the rest
                        quote.Append($"\"{remaining[0..remaining.Length].ToString()}");
                        remaining = remaining.Slice(remaining.Length - 1, 0);
                        break;
                    }

                    quote.Append($"\"{remaining[0..quoteIndex].ToString()}\"");
                    remaining = remaining[(quoteIndex + 1)..remaining.Length];
                    if (remaining.IndexOf("\"") != 0)
                    { 
                        isInQuoteBlock = false;
                    }
                }

                columnValues.Add(quote.ToString());
                if (remaining.Length > 0)
                {
                    remaining = remaining[delimiter.Length..remaining.Length];
                }
            }
        }

        return columnValues;
    }
}
