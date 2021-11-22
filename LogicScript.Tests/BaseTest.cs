using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace LogicScript.Tests
{
    [TestFixtureSource(nameof(BaseTest.Runners))]
    public abstract class BaseTest
    {
        public static IEnumerable<object> Runners = new object[] { new InterpretedRunner(), new CompiledRunner() };

        protected readonly IRunner Runner;

        public BaseTest(IRunner runner)
        {
            this.Runner = runner;
        }
    }
}