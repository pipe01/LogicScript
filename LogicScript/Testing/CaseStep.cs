using System.Collections.Generic;
using LogicScript.Data;
using LogicScript.Parsing.Structures;

namespace LogicScript.Testing
{
    internal record struct PortValue(string Name, PortInfo Port, BitsValue Value);

    internal class CaseStep(IList<PortValue> inputs, IList<PortValue> outputs)
    {
        public IList<PortValue> Inputs { get; } = inputs;
        public IList<PortValue> Outputs { get; } = outputs;
    }
}