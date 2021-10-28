using System;

namespace LogicScript.Interpreting
{
    public class Interpreter
    {
        private readonly Script Script;

        public Interpreter(Script script)
        {
            this.Script = script;
        }

        public void Run(IMachine machine, bool checkPortCount = true)
        {
            if (checkPortCount)
            {
                if (machine.InputCount != Script.RegisteredInputLength)
                    throw new InterpreterException($"Input length mismatch: script requires {Script.RegisteredInputLength} but machine has {machine.InputCount}");

                if (machine.OutputCount != Script.RegisteredOutputLength)
                    throw new InterpreterException($"Output length mismatch: script requires {Script.RegisteredOutputLength} but machine has {machine.OutputCount}");
            }

            machine.AllocateRegisters(Script.Registers.Count);

            Span<bool> input = stackalloc bool[machine.InputCount];
            machine.ReadInput(input);

            var visitor = new Visitor(machine, input);

            foreach (var block in Script.Blocks)
            {
                visitor.Visit(block);
            }
        }
    }
}
