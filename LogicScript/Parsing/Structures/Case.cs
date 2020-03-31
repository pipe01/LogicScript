using System;

namespace LogicScript.Parsing.Structures
{
    internal class Case
    {
        public InputSpec InputSpec { get; }
        public BitsValue InputsValue { get; }
        public Statement[]? Statements { get; }

        public Case(InputSpec inputSpec, BitsValue inputsValue, Statement[]? statements)
        {
            this.InputSpec = inputSpec ?? throw new ArgumentNullException(nameof(inputSpec));
            this.InputsValue = inputsValue;
            this.Statements = statements;
        }
    }
}
