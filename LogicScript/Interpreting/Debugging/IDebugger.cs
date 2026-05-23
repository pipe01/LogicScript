using System.Threading.Tasks;
using LogicScript.Parsing.Structures.Statements;

namespace LogicScript.Interpreting.Debugging
{
    public interface IDebugger
    {
        Task WaitForResumeAsync();

        void Continue();
        void Next();

        void TraceStatement(Interpreter interpreter, Statement stmt, out bool pause);
    }
}
