using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogicScript.Data;

namespace LogicScript.ByteCode
{
    public class CPU
    {
        private ushort[] Program;
        private int Pointer;
        private Stack<BitsValue> Stack = new();

        public CPU(ushort[] program)
        {
            this.Program = program;
        }

        public void Run()
        {
            while (ProcessInstruction()) ;

            if (Stack.Count != 0)
            {
                throw new Exception($"Stack wasn't empty when program ended");
            }
        }

        private bool ProcessInstruction()
        {
            var opcode = (OpCode)Program[Pointer];
            Pointer++;

            switch (opcode)
            {
                case OpCode.Nop:
                    break;

                case OpCode.Pop:
                    Stack.Pop();
                    break;

                case OpCode.Ld_0:
                    Stack.Push(new BitsValue(0, Program[Pointer++]));
                    break;

                case OpCode.Ld_1:
                    Stack.Push(new BitsValue(1, Program[Pointer++]));
                    break;

                case OpCode.Ld_0_1:
                    Stack.Push(BitsValue.Zero);
                    break;

                case OpCode.Ld_1_1:
                    Stack.Push(BitsValue.One);
                    break;

                case OpCode.Ldi_16:
                    Stack.Push(new BitsValue(Program[Pointer++], Program[Pointer++]));
                    break;

                case OpCode.Ldi_32:
                    {
                        uint value = (uint)(Program[Pointer++] << 16) | Program[Pointer++];
                        Stack.Push(new BitsValue(value, Program[Pointer++]));
                        break;
                    }

                case OpCode.Ldi_64:
                    {
                        ulong value =
                              (uint)(Program[Pointer++] << 48)
                            | (uint)(Program[Pointer++] << 32)
                            | (uint)(Program[Pointer++] << 16)
                            | Program[Pointer++];

                        Stack.Push(new BitsValue(value, Program[Pointer++]));
                        break;
                    }

                case OpCode.Dup:
                    Stack.Push(Stack.Peek());
                    break;

                case OpCode.Show:
                    Console.WriteLine($"Debug print: {Stack.Pop()}");
                    break;

                case OpCode.Jmp:
                    Pointer = TakeAddress();
                    break;

                case OpCode.Brz or OpCode.Brnz:
                    if ((Stack.Pop() == 0) == (opcode == OpCode.Brz))
                        Pointer = TakeAddress();
                    else
                        TakeAddress();

                    break;

                case OpCode.Breq or OpCode.Brneq:
                    if ((Stack.Pop() == Stack.Pop()) == (opcode == OpCode.Breq))
                        Pointer = TakeAddress();
                    else
                        TakeAddress();

                    break;

                case OpCode.Add:
                    Stack.Push(Stack.Pop() + Stack.Pop());
                    break;

                case OpCode.Sub:
                    Stack.Push(Stack.Pop() - Stack.Pop());
                    break;

                default:
                    throw new Exception("Invalid instruction");
            }

            return Pointer < Program.Length;
        }

        private int TakeAddress() => (Program[Pointer++] << 16) | Program[Pointer++];
    }
}