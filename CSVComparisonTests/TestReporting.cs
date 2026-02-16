using CSVComparison;
using NUnit.Framework;

namespace CSVComparisonTests;

public class TestReporting
{
    [Test]
    public void TestBreakDetailOutput()
    {
        var breakDetail = new BreakDetail() { BreakDescription = "A break", BreakType=BreakType.ValueMismatch, Column = "COL1"};
        Assert.That(breakDetail.ToString(), Is.EqualTo("Break Type: ValueMismatch. Description: A break"));
    }
}
