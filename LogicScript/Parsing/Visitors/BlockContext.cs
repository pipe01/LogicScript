using LogicScript.Parsing.Structures;
using System.Collections.Generic;

namespace LogicScript.Parsing.Visitors
{
    internal sealed class BlockContext
    {
        public ScriptContext Outer { get; }
        public IDictionary<string, LocalInfo> Locals { get; } = new Dictionary<string, LocalInfo>();

        public bool IsInConstant { get; }

        public BlockContext(ScriptContext outer, bool isInConstant)
        {
            this.Outer = outer;
            this.IsInConstant = isInConstant;
        }

        public bool DoesIdentifierExist(string iden)
            => Locals.ContainsKey(iden)
            || Outer.DoesIdentifierExist(iden);
    }
}
