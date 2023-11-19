using Xunit.Abstractions;
using Xunit.Sdk;

namespace tests.Xunit;

// From: https://learn.microsoft.com/en-us/dotnet/core/testing/order-unit-tests?pivots=xunit
public class AlphabeticalOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(
        IEnumerable<TTestCase> testCases) where TTestCase : ITestCase =>
        testCases.OrderBy(testCase => testCase.TestMethod.Method.Name);
}