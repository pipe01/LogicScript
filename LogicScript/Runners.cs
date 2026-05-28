using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogicScript.Compiling;
using LogicScript.Interpreting;
using LogicScript.Interpreting.Debugging;

namespace LogicScript
{
    public abstract class Runner
    {
        public virtual bool CanRun => false;
        public virtual bool CanRunAsync => false;

        internal Runner()
        {
        }

        public virtual void Run(IMachine machine, Script script, bool runStartup) => throw new NotImplementedException();
        public virtual Task RunAsync(IMachine machine, Script script, bool runStartup, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public static Runner Compiled() => new CompiledRunner();
        public static Runner Interpreted(IDebugger? debugger = null, int statementLimit = -1) => new InterpretedRunner(debugger, statementLimit);
    }

    internal sealed class CompiledRunner : Runner
    {
        public override bool CanRun => true;

        private readonly Dictionary<Script, CompiledScript> CompiledScripts = [];
        private bool[] Scratch = [];

        public override void Run(IMachine machine, Script script, bool runStartup)
        {
            if (!CompiledScripts.TryGetValue(script, out var compiledScript))
                CompiledScripts[script] = compiledScript = Compiler.Compile(script);

            Array.Resize(ref Scratch, Math.Max(machine.InputCount, machine.OutputCount));

            compiledScript(machine, Scratch, runStartup);
        }
    }

    internal sealed class InterpretedRunner(IDebugger? debugger, int statementLimit) : Runner
    {
        public override bool CanRun => true;
        public override bool CanRunAsync => true;

        public override void Run(IMachine machine, Script script, bool runStartup)
        {
            new Interpreter(script, machine, runStartup, debugger: debugger).Run(statementLimit);
        }

        public override async Task RunAsync(IMachine machine, Script script, bool runStartup, CancellationToken cancellationToken)
        {
            await new Interpreter(script, machine, runStartup, debugger: debugger).RunToEndAsync(cancellationToken, statementLimit);
        }
    }
}
