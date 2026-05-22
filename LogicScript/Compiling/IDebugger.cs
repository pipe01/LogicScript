using System.Collections.Generic;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;

namespace LogicScript.Compiling
{
    public interface IDebugger
    {
        void TraceStatement(SourceSpan span, IDictionary<LocalInfo, ulong> locals);
    }
}
