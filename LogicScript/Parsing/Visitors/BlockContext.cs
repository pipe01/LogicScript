using LogicScript.Parsing.Structures;
using System.Collections.Generic;

namespace LogicScript.Parsing.Visitors
{
    internal sealed class BlockContext
    {
        public ScriptContext Outer { get; }
        public IDictionary<string, LocalInfo> Locals { get; } = new Dictionary<string, LocalInfo>();

        public ErrorSink Errors => Outer.Errors;

        public bool IsInConstant { get; }

        public BlockContext(ScriptContext outer, bool isInConstant)
        {
            this.Outer = outer;
            this.IsInConstant = isInConstant;
        }

        public bool DoesIdentifierExist(string iden)
            => Locals.ContainsKey(iden)
            || Outer.DoesIdentifierExist(iden);

        public LocalInfo AddLocal(string name, int size, SourceSpan span)
        {
            var info = new LocalInfo(size, $"{name}_{Outer.LocalCounter++}", name, span);
            Locals.Add(name, info);
            return info;
        }
    }
}
