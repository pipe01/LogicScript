using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal abstract class TaskStatement(SourceSpan span) : Statement(span)
    {
    }

    internal sealed class PrintTaskStatement(SourceSpan span, string text) : TaskStatement(span)
    {
        public string Text { get; set; } = text;
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
