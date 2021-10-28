using LogicScript.Data;
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

        public void Run(IMachine machine)
        {
            machine.AllocateRegisters(Script.Registers.Count);

            Span<bool> input = stackalloc bool[machine.InputCount];
            machine.ReadInput(input);

            foreach (var block in Script.Blocks)
            {
                new Visitor(Script, machine, input).Visit(block);
            }
        }
    }
}
