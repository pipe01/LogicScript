using LogicScript.Compiling;
using LogicScript.Parsing;

namespace LogicScript;

public sealed class DummyDebugger : IDebugger
{
    public static readonly DummyDebugger Instance = new();

    private DummyDebugger() { }

    public void GotOutput(string line)
    {
    }

    public void PushFunctionCall(NodeID id)
    {
    }

    public void PopFunctionCall()
    {
    }

    public void PopLocal(NodeID id)
    {
    }

    public void PushLocal(NodeID id)
    {
    }

    public void SetLocal(NodeID id, ulong value)
    {
    }

    public void TraceStatement(IScriptInstance compiledScript, IMachine machine, NodeID id)
    {
    }
}
