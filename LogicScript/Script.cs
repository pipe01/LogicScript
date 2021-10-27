using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using System.Collections.Generic;
using System.Linq;

namespace LogicScript
{
    public class Script
    {
        internal IDictionary<string, PortInfo> Inputs = new Dictionary<string, PortInfo>();
        internal IDictionary<string, PortInfo> Outputs = new Dictionary<string, PortInfo>();
        internal IDictionary<string, PortInfo> Registers = new Dictionary<string, PortInfo>();

    }
}
