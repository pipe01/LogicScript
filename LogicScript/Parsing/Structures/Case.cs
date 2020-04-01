using System;

namespace LogicScript.Parsing.Structures
{
    internal class Case : ICodeNode
    {
        public InputSpec InputSpec { get; }
        public Expression InputsValue { get; }
        public Statement[]? Statements { get; }
        public SourceLocation Location { get; }

        public Case(InputSpec inputSpec, Expression inputsValue, Statement[]? statements, SourceLocation location)
        {
            this.InputSpec = inputSpec ?? throw new ArgumentNullException(nameof(inputSpec));
            this.InputsValue = inputsValue;
            this.Statements = statements;
            this.Location = location;
        }
    }
}
