using CSVComparison;
using NUnit.Framework;

namespace CSVComparisonTests
{
    public class TestInternals
    {
        [Test]
        public void TestStringSplit()
        {
            var simpleLine = "A,B,C";
            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            var csvComparer = new CSVComparer(comparisonDefinition);

            var columnValues = csvComparer.SplitStringWithQuotes(simpleLine);
            Assert.AreEqual(3, columnValues.Count);
            Assert.AreEqual("C", columnValues[2]);
        }

        [Test]
        public void TestStringSplitEmptyColumns()
        {
            var simpleLine = "A,,";
            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            var csvComparer = new CSVComparer(comparisonDefinition);

            var columnValues = csvComparer.SplitStringWithQuotes(simpleLine);
            Assert.AreEqual(3, columnValues.Count);
            Assert.AreEqual("", columnValues[2]);
        }

        [Test]
        public void TestComplexStringSplit()
        {
            var complexLine = "A,\"B contains a quote, comma\",C";
            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            var csvComparer = new CSVComparer(comparisonDefinition);

            var columnValues = csvComparer.SplitStringWithQuotes(complexLine);
            Assert.AreEqual(3, columnValues.Count);
            Assert.AreEqual("\"B contains a quote, comma\"", columnValues[1]);
            Assert.AreEqual("C", columnValues[2]);
        }

        [Test]
        public void TestComplexStringSplitMultipleCommasInQuotes()
        {
            var complexLine = "A,\"B contains a quote, comma, and another, and another\",C";
            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            var csvComparer = new CSVComparer(comparisonDefinition);

            var columnValues = csvComparer.SplitStringWithQuotes(complexLine);
            Assert.AreEqual(3, columnValues.Count);
            Assert.AreEqual("\"B contains a quote, comma, and another, and another\"", columnValues[1]);
            Assert.AreEqual("C", columnValues[2]);
        }

        [Test]
        public void TestComplexStringSplitMultipleQuotes()
        {
            var complexLine = "A,\"B contains a quote, comma\",\"Also contains a,comma\",D";
            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            var csvComparer = new CSVComparer(comparisonDefinition);

            var columnValues = csvComparer.SplitStringWithQuotes(complexLine);
            Assert.AreEqual(4, columnValues.Count);
            Assert.AreEqual("\"B contains a quote, comma\"", columnValues[1]);
            Assert.AreEqual("\"Also contains a,comma\"", columnValues[2]);
            Assert.AreEqual("D", columnValues[3]);
        }


        [Test]
        public void TestComplexStringSplitQuoteNotClosed()
        {
            var complexLine = "A,\"B contains a quote, comma,C,D";
            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            var csvComparer = new CSVComparer(comparisonDefinition);

            var columnValues = csvComparer.SplitStringWithQuotes(complexLine);
            Assert.AreEqual(2, columnValues.Count);
            Assert.AreEqual("\"B contains a quote, comma,C,D", columnValues[1]);
        }

        [Test]
        public void TestQuoteAsLastCharacter()
        {
            var complexLine = "A,B,\"C,D\"";
            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            var csvComparer = new CSVComparer(comparisonDefinition);

            var columnValues = csvComparer.SplitStringWithQuotes(complexLine);
            Assert.AreEqual(3, columnValues.Count);
        }

        [Test]
        public void TestQuoteInsideAField()
        {
            var complexLine = "A,B,\"C A Field with \"\" quotes\",D";
            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            var csvComparer = new CSVComparer(comparisonDefinition);

            var columnValues = csvComparer.SplitStringWithQuotes(complexLine);
            Assert.AreEqual(4, columnValues.Count);
            Assert.AreEqual("\"C A Field with \"\" quotes\"", columnValues[2]);

            complexLine = "A,B,\"C A Field with \"\" quotes, and comma\",D";
            columnValues = csvComparer.SplitStringWithQuotes(complexLine);
            Assert.AreEqual(4, columnValues.Count);
            Assert.AreEqual("\"C A Field with \"\" quotes, and comma\"", columnValues[2]);
        }

        [Test]
        public void TestMultipleQuotesInsideAField()
        {
            var complexLine = "A,B,\"C A Field with \"\"\"\" quotes\",D";
            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            var csvComparer = new CSVComparer(comparisonDefinition);

            var columnValues = csvComparer.SplitStringWithQuotes(complexLine);
            Assert.AreEqual(4, columnValues.Count);
            Assert.AreEqual("\"C A Field with \"\"\"\" quotes\"", columnValues[2]);
        }

        [Test]
        public void TestMultipleQuotesAtStartofField()
        {
            var complexLine = "A,B,\"\"\"C A Field with starting quotes\",D";
            var comparisonDefinition = new ComparisonDefinition() { Delimiter = "," };
            var csvComparer = new CSVComparer(comparisonDefinition);

            var columnValues = csvComparer.SplitStringWithQuotes(complexLine);
            Assert.AreEqual(4, columnValues.Count);
            Assert.AreEqual("\"\"\"C A Field with starting quotes\"", columnValues[2]);
        }
    }
}
