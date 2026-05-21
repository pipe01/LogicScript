using LogicScript.Parsing.Structures;
using System.Collections.Generic;

namespace LogicScript.Parsing.Visitors
{
    internal sealed class BlockContext
    {
        public ScriptContext Script { get; }
        public BlockContext? Outer { get; }
        public IDictionary<string, LocalInfo> Locals { get; } = new Dictionary<string, LocalInfo>();

        public ErrorSink Errors => Script.Errors;

        public bool IsInConstant { get; }
        public NodeID? LoopID { get; }

        public BlockContext(ScriptContext script, BlockContext? outer = null, bool isInConstant = false, NodeID? loopID = null)
        {
            this.Script = script;
            this.Outer = outer;
            this.IsInConstant = isInConstant;
            this.LoopID = loopID;
        }

        public bool DoesIdentifierExist(string iden)
            => TryGetLocal(iden, out _)
            || Script.DoesIdentifierExist(iden);

        public LocalInfo AddLocal(string name, int size, SourceSpan span)
        {
            var info = new LocalInfo(size, $"{name}_{Script.LocalCounter++}", name, span);
            Locals.Add(name, info);
            return info;
        }

        public bool TryGetLocal(string name, out LocalInfo local)
        {
            if (Locals.TryGetValue(name, out local))
                return true;

            if (Outer != null && Outer.TryGetLocal(name, out local))
                return true;

            return false;
        }
    }
}
