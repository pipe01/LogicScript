using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Statements
{
    internal abstract class TaskStatement : Statement
    {
        protected TaskStatement(SourceSpan span) : base(span)
        {
        }
    }

    internal sealed class PrintTaskStatement : TaskStatement
    {
        public string Text { get; set; }

        public PrintTaskStatement(SourceSpan span, string text) : base(span)
        {
            this.Text = text;
        }
    }

    internal sealed class ShowTaskStatement : TaskStatement
    {
        public Expression Value { get; set; }

        public ShowTaskStatement(SourceSpan span, Expression value) : base(span)
        {
            this.Value = value;
        }

        protected override IEnumerator<ICodeNode> GetChildren()
        {
            yield return Value;
        }
    }
}
