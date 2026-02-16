using CSVComparison;
using NUnit.Framework;

namespace CSVComparisonTests;

public class TestInternals
{
    [Test]
    public void TestStringSplit()
    {
        var simpleLine = "A,B,C";  
        var columnValues = RowHelper.SplitRowWithQuotes(simpleLine, ",");
        Assert.That(columnValues.Count, Is.EqualTo(3));
        Assert.That(columnValues[2], Is.EqualTo("C"));
    }

    [Test]
    public void TestStringSplitEmptyColumns()
    {
        var simpleLine = "A,,";
        var columnValues = RowHelper.SplitRowWithQuotes(simpleLine, ",");
        Assert.That(columnValues.Count, Is.EqualTo(3));
        Assert.That(columnValues[2], Is.EqualTo(""));
    }

    [Test]
    public void TestComplexStringSplit()
    {
        var complexLine = "A,\"B contains a quote, comma\",C";
        var columnValues = RowHelper.SplitRowWithQuotes(complexLine, ",");
        Assert.That(columnValues.Count, Is.EqualTo(3));
        Assert.That(columnValues[1], Is.EqualTo("\"B contains a quote, comma\""));
        Assert.That(columnValues[2], Is.EqualTo("C"));
    }

    [Test]
    public void TestComplexStringSplitMultipleCommasInQuotes()
    {
        var complexLine = "A,\"B contains a quote, comma, and another, and another\",C";
        var columnValues = RowHelper.SplitRowWithQuotes(complexLine, ",");
        Assert.That(columnValues.Count, Is.EqualTo(3));
        Assert.That(columnValues[1], Is.EqualTo("\"B contains a quote, comma, and another, and another\""));
        Assert.That(columnValues[2], Is.EqualTo("C"));
    }

    [Test]
    public void TestComplexStringSplitMultipleQuotes()
    {
        var complexLine = "A,\"B contains a quote, comma\",\"Also contains a,comma\",D";
        var columnValues = RowHelper.SplitRowWithQuotes(complexLine, ",");
        Assert.That(columnValues.Count, Is.EqualTo(4));
        Assert.That(columnValues[1], Is.EqualTo("\"B contains a quote, comma\""));
        Assert.That(columnValues[2], Is.EqualTo("\"Also contains a,comma\""));
        Assert.That(columnValues[3], Is.EqualTo("D"));
    }

    [Test]
    public void TestComplexStringSplitQuoteNotClosed()
    {
        var complexLine = "A,\"B contains a quote, comma,C,D";
        var columnValues = RowHelper.SplitRowWithQuotes(complexLine, ",");
        Assert.That(columnValues.Count, Is.EqualTo(2));
        Assert.That(columnValues[1], Is.EqualTo("\"B contains a quote, comma,C,D"));
    }

    [Test]
    public void TestQuoteAsLastCharacter()
    {
        var complexLine = "A,B,\"C,D\"";
        var columnValues = RowHelper.SplitRowWithQuotes(complexLine, ",");
        Assert.That(columnValues.Count, Is.EqualTo(3));
    }

    [Test]
    public void TestQuoteInsideAField()
    {
        var complexLine = "A,B,\"C A Field with \"\" quotes\",D";
        var columnValues = RowHelper.SplitRowWithQuotes(complexLine, ",");
        Assert.That(columnValues.Count, Is.EqualTo(4));
        Assert.That(columnValues[2], Is.EqualTo("\"C A Field with \"\" quotes\""));

        complexLine = "A,B,\"C A Field with \"\" quotes, and comma\",D";
        columnValues = RowHelper.SplitRowWithQuotes(complexLine, ",");
        Assert.That(columnValues.Count, Is.EqualTo(4));
        Assert.That(columnValues[2], Is.EqualTo("\"C A Field with \"\" quotes, and comma\""));
    }

    [Test]
    public void TestMultipleQuotesInsideAField()
    {
        var complexLine = "A,B,\"C A Field with \"\"\"\" quotes\",D";
        var columnValues = RowHelper.SplitRowWithQuotes(complexLine, ",");
        Assert.That(columnValues.Count, Is.EqualTo(4));
        Assert.That(columnValues[2], Is.EqualTo("\"C A Field with \"\"\"\" quotes\""));
    }

    [Test]
    public void TestMultipleQuotesAtStartofField()
    {
        var complexLine = "A,B,\"\"\"C A Field with starting quotes\",D";
        var columnValues = RowHelper.SplitRowWithQuotes(complexLine, ",");
        Assert.That(columnValues.Count, Is.EqualTo(4));
        Assert.That(columnValues[2], Is.EqualTo("\"\"\"C A Field with starting quotes\""));
    }

    [Test]
    public void Split_With_LongStringDelimiter()
    {
        var complexLine = "A##\"B contains a quote##comma\"##\"Also contains a##comma\"##D";
        var columnValues = RowHelper.SplitRowWithQuotes(complexLine, "##");
        Assert.That(columnValues.Count, Is.EqualTo(4));
        Assert.That(columnValues[0], Is.EqualTo("A"));
        Assert.That(columnValues[1], Is.EqualTo("\"B contains a quote##comma\""));
        Assert.That(columnValues[2], Is.EqualTo("\"Also contains a##comma\""));
        Assert.That(columnValues[3], Is.EqualTo("D"));
    }
}
