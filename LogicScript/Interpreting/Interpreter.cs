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
    public enum ExitReason
    {
        Ended,
        Debugger,
        LimitReached,
    }

    [Serializable]
    public class InterpreterLimitReachedException : Exception;

    public partial class Interpreter
    {
        private readonly struct Operation(ICodeNode? node, bool breakBarrier = false, Func<bool>? before = null, Action? after = null)
        {
            public readonly bool BreakBarrier = breakBarrier;
            public readonly ICodeNode? Node = node;
            public readonly Func<bool>? Before = before;
            public readonly Action? After = after;
        }

        public IMachine? Machine { get; }
        private readonly IDebugger? Debugger;
        public Script? Script { get; }

        private readonly Stack<Operation> OpStack = [];
        private readonly Dictionary<LocalInfo, BitsValue> Locals = [];

        public Statement? CurrentLocation => OpStack.TryPeek(out var op) && op.Node is Statement stmt ? stmt : null;

        private Interpreter(Script? script, IMachine? machine, IDebugger? debugger)
        {
            this.Script = script;
            this.Machine = machine;
            this.Debugger = debugger;
        }

        public Interpreter(Script script, IMachine machine, bool runStartup, bool checkPortCount = true, IDebugger? debugger = null) : this(script, machine, debugger)
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

            return new Interpreter(null, null, null).Visit(expr);
        }

        public ExitReason Run(int statementLimit = -1)
        {
            while (OpStack.TryPop(out var op))
            {
                if (op.Node is Statement s && op.Node is not BlockStatement && Debugger != null)
                {
                    Debugger.TraceStatement(this, s, out var pause);
                    if (pause)
                    {
                        OpStack.Push(op);
                        return ExitReason.Debugger;
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

                if (statementLimit >= 0 && --statementLimit == 0)
                {
                    return ExitReason.LimitReached;
                }
            }

            return ExitReason.Ended;
        }

        public async Task RunToEndAsync(int statementLimit = -1)
        {
            while (true)
            {
                var exitReason = Run(statementLimit);
                if (exitReason == ExitReason.Debugger && Debugger != null)
                    await Debugger.WaitForResumeAsync();
                else if (exitReason == ExitReason.LimitReached)
                    throw new InterpreterLimitReachedException();
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

        public (BitsValue Value, IReadOnlyCollection<Error> ParseErrors) Evaluate(string expression)
        {
            if (Script == null)
                throw new InvalidOperationException("Can't evaluate in constant context");

            var (parsed, errors) = Script.ParseExpression(expression, Script, Locals.Keys);
            if (errors.Count > 0)
            {
                return (0, errors);
            }

            return (Visit(parsed!), []);
        }
    }
}
