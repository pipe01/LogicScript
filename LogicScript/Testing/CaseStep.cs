using System.Collections.Generic;
using System.Linq;
using LogicScript.Data;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;

namespace LogicScript.Testing
{
    public readonly record struct PortValue(BitsValue Value, SourceSpan Span) : ICodeNode
    {
        public readonly IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }

        public override string ToString() => Value.ToString();
    }

    public readonly record struct PortValues(string Name, MachinePorts Ports, PortValue[] Values, SourceSpan NameSpan) : ICodeNode
    {
        readonly SourceSpan ICodeNode.Span => NameSpan;

        public SourceSpan ValuesSpan => new(Values[0].Span.Start, Values[^1].Span.End);

        public IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }

        public override string ToString() => $"{Name}({string.Join(", ", Values)})";
    }

    public record CaseStep(IList<PortValues> Inputs, IList<PortValues> Outputs, SourceSpan Span) : ICodeNode
    {
        public IEnumerable<ICodeNode> GetChildren()
        {
            return Inputs.Concat(Outputs).Cast<ICodeNode>();
        }

        public override string ToString() => $"{string.Join(' ', Inputs)} => {string.Join(' ', Outputs)}";
    }
}
