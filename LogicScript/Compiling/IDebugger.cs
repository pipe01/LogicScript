using LogicScript.Parsing;

namespace LogicScript.Compiling
{
    public interface IDebugger
    {
        void PushLocal(NodeID id);
        void SetLocal(NodeID id, ulong value);
        void PopLocal(NodeID id);

        void TraceStatement(IScriptInstance compiledScript, IMachine machine, NodeID id);

        void GotOutput(string line);
    }
}