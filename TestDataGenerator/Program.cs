using System;
using System.IO;

namespace TestDataGenerator
{
    class Program
    {
        static string[] columnNames = new [] { "COL A,COL B,COL C,COL D" };
        static string[] colBValues = new string[] { "A", "B", "C", "D", "AA", "BB", "CC", "DD" };
        static string[] colCValues = new string[] { "E", "F", "G", "H", "EE", "FF", "GG", "HH"};
        static double[] colDValues = new double[] { 1.5, 10.5, 32.1, 42.0, -9, -9.05, 999.6 };

        /// <summary>
        /// Tool to create sample csv test files. There are 4 columns, COL A is the index column and the remaining 3 are populated 
        /// with data picked randomly from arrays.
        /// 
        /// Use this configuration:
        /// 
        /// <ComparisonDefinition xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
        ///    <Delimiter>,</Delimiter>
        ///    <KeyColumns>
        ///       <Column>COL A</Column>
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
                using (var leftHandSideWriter = new StreamWriter(@"C:\temp\leftHandSideTest.csv"))
                using (var rightHandSideWriter = new StreamWriter(@"C:\temp\rightHandSideTest.csv"))
                {
                    // Write header
                    leftHandSideWriter.WriteLine(string.Join(',', columnNames));
                    rightHandSideWriter.WriteLine(string.Join(',', columnNames));

                    for (var index = 0; index < numberOfRows; index++)
                    {
                        var bIndex = random.Next(0, colBValues.Length - 1);
                        var cIndex = random.Next(0, colCValues.Length - 1);
                        var dIndex = random.Next(0, colDValues.Length - 1);

                        var row = $"{index},{colBValues[bIndex]},{colCValues[cIndex]},{colDValues[dIndex]}";
                        leftHandSideWriter.WriteLine(row);

                        // Add a break to the canidate
                        if (index > 0 && index % 100 == 0)
                        {
                            bIndex = random.Next(0, colBValues.Length - 1);
                            cIndex = random.Next(0, colCValues.Length - 1);
                            dIndex = random.Next(0, colDValues.Length - 1);
                            row = $"{index},{colBValues[bIndex]},{colCValues[cIndex]},{colDValues[dIndex]}";
                        }

                        rightHandSideWriter.WriteLine(row);
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
