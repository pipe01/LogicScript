using System;

namespace LogicScript.Parsing.Structures
{
    internal class Case
    {
        public InputSpec InputSpec { get; }
        public InputValSpec InputValSpec { get; }
        public Statement[]? Statements { get; }

        public Case(InputSpec inputSpec, InputValSpec inputValSpec, Statement[]? statements)
        {
            this.InputSpec = inputSpec ?? throw new ArgumentNullException(nameof(inputSpec));
            this.InputValSpec = inputValSpec ?? throw new ArgumentNullException(nameof(inputValSpec));
            this.Statements = statements;
        }
    }
}
