using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing.Visitors
{
    internal sealed class ScriptContext
    {
        public Script Script { get; }
        public ErrorSink Errors { get; }
        public IDictionary<string, Expression> Constants { get; } = new Dictionary<string, Expression>();
        public int LocalCounter { get; set; }

        public ScriptContext(Script script, ErrorSink errors)
        {
            this.Script = script;
            this.Errors = errors;
        }

        public bool DoesIdentifierExist(string iden)
            => Script.Inputs.ContainsKey(iden)
            || Script.Outputs.ContainsKey(iden)
            || Script.Registers.ContainsKey(iden);
    }
}
