﻿using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Parsing.Structures.Statements
{
    internal sealed class DeclareLocalStatement : Statement
    {
        public string Name { get; set; }
        public int Size { get; set; }
        public Expression? Initializer { get; set; }

        public DeclareLocalStatement(SourceSpan span, string name, int size, Expression? initializer) : base(span)
        {
            this.Name = name;
            this.Size = size;
            this.Initializer = initializer;
        }
    }
}
