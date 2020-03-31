using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    internal class Script
    {
        public IList<Case> Cases { get; } = new List<Case>();
    }
}
