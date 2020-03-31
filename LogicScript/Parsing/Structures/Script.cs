using System.Collections.Generic;

namespace LogicScript.Parsing.Structures
{
    public class Script
    {
        internal IList<Case> Cases { get; } = new List<Case>();
    }
}
