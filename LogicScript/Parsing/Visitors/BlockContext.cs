using LogicScript.Parsing.Structures;
using System.Collections.Generic;

namespace LogicScript.Parsing.Visitors
{
    internal sealed class BlockContext(ScriptContext script, BlockContext? outer = null, bool isInConstant = false, NodeID? loopID = null)
    {
        public ScriptContext Script { get; } = script;
        public BlockContext? Outer { get; } = outer;
        public IDictionary<string, LocalInfo> Locals { get; } = new Dictionary<string, LocalInfo>();

        public ErrorSink Errors => Script.Errors;

        public bool IsInConstant { get; } = isInConstant;
        public NodeID? LoopID { get; } = loopID;

        public bool DoesIdentifierExist(string iden)
            => TryGetLocal(iden, out _)
            || Script.DoesIdentifierExist(iden);

        public LocalInfo AddLocal(string name, int size, SourceSpan span)
        {
            var info = new LocalInfo(NodeID.Next(), size, name, span);
            Locals.Add(name, info);
            return info;
        }

        public bool TryGetLocal(string name, out LocalInfo local, bool checkOuter = true)
        {
            if (Locals.TryGetValue(name, out local))
                return true;

            if (checkOuter && Outer != null && Outer.TryGetLocal(name, out local))
                return true;

            return false;
        }
    }
}
