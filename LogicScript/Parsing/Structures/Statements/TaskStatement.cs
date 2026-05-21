using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Utils;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal abstract class TaskStatement(SourceSpan span) : Statement(span)
    {
    }

    internal sealed class PrintTaskStatement(SourceSpan span, PrintStringFormat str) : TaskStatement(span)
    {
        // For tests only
        public PrintTaskStatement(string str) : this(default, PrintStringFormat.Parse(str)) { }

        public PrintStringFormat String { get; set; } = str;
    }

    internal sealed class ShowTaskStatement(SourceSpan span, Expression value) : TaskStatement(span)
    {
        public Expression Value { get; set; } = value;

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Value;
        }
    }

    internal sealed class UpdateTaskStatement(SourceSpan span) : TaskStatement(span)
    {
    }
}
