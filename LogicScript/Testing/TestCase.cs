using System.Collections.Generic;

namespace LogicScript.Testing
{
    internal class TestCase
    {
        public string Name { get; }
        public IList<CaseStep> Steps { get; }

        public TestCase(string name, IList<CaseStep> steps)
        {
            this.Name = name;
            this.Steps = steps;
        }
    }
}