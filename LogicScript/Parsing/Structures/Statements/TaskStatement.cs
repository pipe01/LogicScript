using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Utils;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal abstract class TaskStatement(NodeID id, SourceSpan span) : Statement(id, span)
    {
    }

    internal sealed class PrintTaskStatement(NodeID id, SourceSpan span, PrintStringFormat str) : TaskStatement(id, span)
    {
        // For tests only
        public PrintTaskStatement(NodeID id, string str) : this(id, default, PrintStringFormat.Parse(default, str)) { }

        public PrintStringFormat String { get; set; } = str;

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return String;
        }
    }

    internal sealed class ShowTaskStatement(NodeID id, SourceSpan span, Expression value) : TaskStatement(id, span)
    {
        public Expression Value { get; set; } = value;

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Value;
        }
    }

    internal sealed class UpdateTaskStatement(NodeID id, SourceSpan span) : TaskStatement(id, span)
    {
    }
}
