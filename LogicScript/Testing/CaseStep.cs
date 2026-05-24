using System.Collections.Generic;
using System.Linq;
using LogicScript.Data;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;

namespace LogicScript.Testing
{
    public record struct PortValue(string Name, MachinePorts Ports, BitsValue Value, SourceSpan NameSpan, SourceSpan ValueSpan) : ICodeNode
    {
        readonly SourceSpan ICodeNode.Span => NameSpan;

        public readonly IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }
    }

    public record CaseStep(IList<PortValue> Inputs, IList<PortValue> Outputs, SourceSpan Span) : ICodeNode
    {
        public IEnumerable<ICodeNode> GetChildren()
        {
            return Inputs.Concat(Outputs).Cast<ICodeNode>();
        }
    }
}
