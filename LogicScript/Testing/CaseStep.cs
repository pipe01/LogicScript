using System.Collections.Generic;
using LogicScript.Data;
using LogicScript.Parsing.Structures;

namespace LogicScript.Testing
{
    internal record struct PortValue(string Name, PortInfo Port, BitsValue Value);

    internal class CaseStep
    {
        public IList<PortValue> Inputs { get; } = new List<PortValue>();
        public IList<PortValue> Outputs { get; } = new List<PortValue>();

        public CaseStep(IList<PortValue> inputs, IList<PortValue> outputs)
        {
            this.Inputs = inputs;
            this.Outputs = outputs;
        }
    }
}