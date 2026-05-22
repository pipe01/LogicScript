using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing.Visitors
{
    internal sealed class ScriptContext(Script script, ErrorSink errors)
    {
        public Script Script { get; } = script;
        public ErrorSink Errors { get; } = errors;
        public IDictionary<string, Expression> Constants { get; } = new Dictionary<string, Expression>();
        public int LocalCounter { get; set; }

        public bool DoesIdentifierExist(string iden)
            => Script.Inputs.ContainsKey(iden)
            || Script.Outputs.ContainsKey(iden)
            || Script.Registers.ContainsKey(iden);
    }
}
