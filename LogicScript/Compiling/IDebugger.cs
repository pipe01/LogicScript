using LogicScript.Parsing;

namespace LogicScript.Compiling
{
    public interface IDebugger2
    {
        void PushLocal(string name);
        void SetLocal(string name, ulong value);
        void PopLocal();

        void TraceStatement(NodeID id);
    }
}