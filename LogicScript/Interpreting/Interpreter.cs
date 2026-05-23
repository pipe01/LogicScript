using LogicScript.Data;
using LogicScript.Interpreting.Debugging;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Structures.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicScript.Interpreting
{
    public partial class Interpreter
    {
        private readonly struct Operation(ICodeNode? node, bool breakBarrier = false, Func<bool>? before = null, Action? after = null)
        {
            public readonly bool BreakBarrier = breakBarrier;
            public readonly ICodeNode? Node = node;
            public readonly Func<bool>? Before = before;
            public readonly Action? After = after;
        }

        private readonly IMachine? Machine;
        private readonly IDebugger? Debugger;

        private readonly Stack<Operation> OpStack = [];
        private readonly Dictionary<LocalInfo, BitsValue> Locals = [];

        public Statement? CurrentLocation => OpStack.TryPeek(out var op) && op.Node is Statement stmt ? stmt : null;

        private Interpreter(IMachine? machine, IDebugger? debugger)
        {
            this.Machine = machine;
            this.Debugger = debugger;
        }

        public Interpreter(Script script, IMachine machine, bool runStartup, bool checkPortCount = true, IDebugger? debugger = null) : this(machine, debugger)
        {
            if (script.HasErrors)
                throw new InterpreterException("Script has errors");

            if (checkPortCount)
            {
                if (machine.InputCount != script.RegisteredInputLength)
                    throw new InterpreterException($"Input length mismatch: script requires {script.RegisteredInputLength} but machine has {machine.InputCount}");

                if (machine.OutputCount != script.RegisteredOutputLength)
                    throw new InterpreterException($"Output length mismatch: script requires {script.RegisteredOutputLength} but machine has {machine.OutputCount}");
            }

            machine.AllocateRegisters(script.Registers.Count);

            foreach (var block in script.Blocks.Reverse())
            {
                if (block is StartupBlock && !runStartup)
                    continue;

                Push(block);
            }
        }

        internal static BitsValue GetConstantValue(Expression expr)
        {
            if (!expr.IsConstant)
                throw new InvalidOperationException("Expression is not constant");

            return new Interpreter(null, null).Visit(expr);
        }

        /// <returns>true if execution paused due to a breakpoint, false otherwise</returns>
        public bool Run()
        {
            while (OpStack.TryPop(out var op))
            {
                if (op.Node is Statement s && op.Node is not BlockStatement && Debugger != null)
                {
                    Debugger.TraceStatement(this, s, out var pause);
                    if (pause)
                    {
                        OpStack.Push(op);
                        return true;
                    }
                }

                if (op.Before?.Invoke() != false)
                {
                    switch (op.Node)
                    {
                        case Block b:
                            Visit(b);
                            break;

                        case Statement stmt:
                            ExecuteStatement(stmt);
                            break;
                    }
                }

                op.After?.Invoke();
            }

            return false;
        }

        public async Task RunToEndAsync()
        {
            while (true)
            {
                var paused = Run();
                if (paused && Debugger != null)
                    await Debugger.WaitForResumeAsync();
                else
                    break;
            }
        }

        public IReadOnlyCollection<(LocalInfo Local, BitsValue Value)> GetAllLocals()
        {
            return [.. Locals.Select(e => (e.Key, e.Value))];
        }

        private void Push(ICodeNode node) => OpStack.Push(new(node));

        private void ClearToBreak()
        {
            while (!OpStack.Pop().BreakBarrier) { }
        }
    }
}
