using System.Collections.Generic;

namespace LogicScript.Testing
{
    internal record class TestCase(int Index, string? Name, IList<CaseStep> Steps)
    {
    }
}