using System.Collections.Generic;
using LogicScript.Parsing.Structures.Statements;

namespace LogicScript.Parsing.Structures.Blocks
{
    internal class FunctionBlock(NodeID id, SourceSpan span, string name, SourceSpan nameSpan, int resultSize, LocalInfo[] parameters, Statement? body) : Block(span), IIdentifiableCodeNode
    {
        public NodeID ID { get; } = id;
        public string Name { get; } = name;
        public SourceSpan NameSpan { get; } = nameSpan;
        public int ResultSize { get; } = resultSize;
        public LocalInfo[] Parameters { get; } = parameters;
        public Statement? Body { get; } = body;

        public bool IsDefined => Body != null;

        public override IEnumerable<ICodeNode> GetChildren()
        {
            foreach (var param in Parameters)
                yield return param;

            if (Body != null)
                yield return Body;
        }
    }
}
