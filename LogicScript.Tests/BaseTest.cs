using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LogicScript.Tests
{
    [TestFixtureSource(nameof(BaseTest.Data))]
    public abstract class BaseTest
    {
        public static IEnumerable<object> Data = Runners.All;

        protected readonly IRunner Runner;

        public BaseTest(IRunner runner)
        {
            this.Runner = runner;
        }

        [SetUp]
        public void Setup()
        {
            TestContext.Write("Running using " + Runner.GetType().Name);
        }
    }
}