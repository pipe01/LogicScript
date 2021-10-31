using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

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

        public override IEnumerable<ICodeNode> GetChildren()
        {
            if (Initializer != null)
                yield return Initializer;
        }
    }
}
