using System;

namespace LogicScript.Parsing.Structures
{
    internal class Case : ICodeNode
    {
        public Expression Condition { get; }
        public Expression InputsValue { get; }
        public Statement[] Statements { get; }
        public SourceLocation Location { get; }

        public Case(Expression condition, Expression inputsValue, Statement[] statements, SourceLocation location)
        {
            this.Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            this.InputsValue = inputsValue;
            this.Statements = statements;
            this.Location = location;
        }
    }
}
