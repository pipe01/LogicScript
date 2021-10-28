using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Parsing.Structures.Statements
{
    internal abstract class TaskStatement : Statement
    {
        public TaskStatement(SourceLocation location) : base(location)
        {
        }
    }

    internal sealed class PrintTaskStatement : TaskStatement
    {
        public string Text { get; set; }

        public PrintTaskStatement(SourceLocation location, string text) : base(location)
        {
            this.Text = text;
        }
    }

    internal sealed class ShowTaskStatement : TaskStatement
    {
        public Expression Value { get; set; }

        public ShowTaskStatement(SourceLocation location, Expression value) : base(location)
        {
            this.Value = value;
        }
    }
}
