using System.Collections.Generic;

namespace LogicScript.Testing
{
    internal class TestCase(string name, IList<CaseStep> steps)
    {
        public string Name { get; } = name;
        public IList<CaseStep> Steps { get; } = steps;
    }
}