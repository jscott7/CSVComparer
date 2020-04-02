using System;
using System.IO;

namespace TestDataGenerator
{
    class Program
    {
        static string[] colBValues = new string[] { "A", "B", "C", "D" };
        static string[] colCValues = new string[] { "E", "F", "G", "H" };
        static double[] colDValues = new double[] { 1.5, 10.5, 32.1, 42.0 };

        /// <summary>
        /// Tool to create sample csv test files. There are 4 columns, COL A is the index column and the remaining 3 are populated 
        /// with data picked randomly from arrays.
        /// 
        /// Use this configuration:
        /// 
        /// <ComparisonDefinition xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
        ///    <Delimiter>,</Delimiter>
        ///    <KeyColumns>
        ///       <Column>A</Column>
        ///    </KeyColumns>
        ///    <HeaderRowIndex>0</HeaderRowIndex>
        ///    <ToleranceValue>0.1</ToleranceValue>
        ///    <ToleranceType>Relative</ToleranceType>
        /// </ComparisonDefinition>
        ///    
        /// </summary>
        /// 
        /// <param name="args">[0] The number of rows to generate</param>
        static void Main(string[] args)
        { 
            try
            {
                int numberOfRows = int.Parse(args[0]);
                var random = new Random();
                using (StreamWriter referenceWriter = new StreamWriter(@"C:\temp\referenceTest.csv"))
                using (StreamWriter candidateWriter = new StreamWriter(@"C:\temp\candidateTest.csv"))
                {
                    // Write header
                    referenceWriter.WriteLine("COL A,COL B,COL C,COL D");
                    candidateWriter.WriteLine("COL A,COL B,COL C,COL D");

                    for (int index = 0; index < numberOfRows; index++)
                    {
                        int bIndex = random.Next(0, 3);
                        int cIndex = random.Next(0, 3);
                        int dIndex = random.Next(0, 3);

                        string row = $"{index},{colBValues[bIndex]},{colCValues[cIndex]},{colDValues[dIndex]}";
                        referenceWriter.WriteLine(row);

                        if (index > 0 && index % 100 == 0)
                        {
                            bIndex = random.Next(0, 3);
                            cIndex = random.Next(0, 3);
                            dIndex = random.Next(0, 3);
                            row = $"{index},{colBValues[bIndex]},{colCValues[cIndex]},{colDValues[dIndex]}";
                        }

                        candidateWriter.WriteLine(row);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
