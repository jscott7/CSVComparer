using CSVComparison;
using NUnit.Framework;

namespace CSVComparisonTests;

public class TestReporting
{
    [Test]
    public void TestBreakDetailOutput()
    {
        var breakDetail = new BreakDetail() { BreakDescription = "A break", BreakType=BreakType.ValueMismatch, Column = "COL1"};
        Assert.AreEqual("Break Type: ValueMismatch. Description: A break", breakDetail.ToString());
    }
}
