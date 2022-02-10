﻿using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MediatR.Extensions.Tests
{
    public class TestMethodNameOrderer : ITestCaseOrderer
    {
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase
        {
            return testCases.OrderBy(t => t.TestMethod.Method.Name);
        }
    }
}