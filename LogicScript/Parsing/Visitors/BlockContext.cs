using LogicScript.Parsing.Structures;
using System.Collections.Generic;

namespace LogicScript.Parsing.Visitors
{
    internal sealed class BlockContext
    {
        public VisitContext Outer { get; }
        public IDictionary<string, LocalInfo> Locals { get; } = new Dictionary<string, LocalInfo>();

        public BlockContext(VisitContext outer)
        {
            this.Outer = outer;
        }
    }
}
