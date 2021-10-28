using LogicScript.Parsing.Structures;
using System.Collections.Generic;

namespace LogicScript
{
    public class Script
    {
        internal IDictionary<string, PortInfo> Inputs { get; } = new Dictionary<string, PortInfo>();
        internal IDictionary<string, PortInfo> Outputs { get; } = new Dictionary<string, PortInfo>();
        internal IDictionary<string, PortInfo> Registers { get; } = new Dictionary<string, PortInfo>();

        internal IList<WhenBlock> Blocks { get; } = new List<WhenBlock>();
    }
}
