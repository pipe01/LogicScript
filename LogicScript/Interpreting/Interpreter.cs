using LogicScript.Parsing.Structures.Blocks;
using System;

namespace LogicScript.Interpreting
{
    internal static class Interpreter
    {
        public static void Run(Script script, IMachine machine, bool runStartup, bool checkPortCount = true)
        {
            if (checkPortCount)
            {
                if (machine.InputCount != script.RegisteredInputLength)
                    throw new InterpreterException($"Input length mismatch: script requires {script.RegisteredInputLength} but machine has {machine.InputCount}");

                if (machine.OutputCount != script.RegisteredOutputLength)
                    throw new InterpreterException($"Output length mismatch: script requires {script.RegisteredOutputLength} but machine has {machine.OutputCount}");
            }

			int registerSum = 0;
			foreach (var register in script.Registers)
				registerSum += register.BitSize;
            machine.AllocateRegisters(registerSum);

            Span<bool> input = stackalloc bool[machine.InputCount];
            machine.ReadInput(input);

            foreach (var block in script.Blocks)
            {
                if (block is StartupBlock && !runStartup)
                    continue;

                var visitor = new Visitor(machine, input);

                visitor.Visit(block);
            }
        }
    }
}
