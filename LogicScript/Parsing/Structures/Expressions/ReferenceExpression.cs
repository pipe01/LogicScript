﻿namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class ReferenceExpression : Expression
    {
        public Reference Target { get; set; }

        public ReferenceExpression(SourceLocation location, Reference target) : base(location)
        {
            this.Target = target;
        }
    }
}