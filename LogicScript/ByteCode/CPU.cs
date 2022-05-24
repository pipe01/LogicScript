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
            switch ((OpCode)Program[Pointer])
            {
                case OpCode.Ld_0:
                    Pointer++;
                    Stack.Push(new BitsValue(0, Program[Pointer++]));
                    break;

                case OpCode.Ld_1:
                    Pointer++;
                    Stack.Push(new BitsValue(1, Program[Pointer++]));
                    break;

                case OpCode.Ld_0_1:
                    Pointer++;
                    Stack.Push(BitsValue.Zero);
                    break;

                case OpCode.Ld_1_1:
                    Pointer++;
                    Stack.Push(BitsValue.One);
                    break;

                case OpCode.Ldi_16:
                    Pointer++;
                    Stack.Push(new BitsValue(Program[Pointer++], Program[Pointer++]));
                    break;

                case OpCode.Ldi_32:
                    {
                        Pointer++;
                        uint value = (uint)(Program[Pointer++] << 16) | Program[Pointer++];
                        Stack.Push(new BitsValue(value, Program[Pointer++]));
                        break;
                    }

                case OpCode.Ldi_64:
                    {
                        Pointer++;
                        ulong value =
                              (uint)(Program[Pointer++] << 48)
                            | (uint)(Program[Pointer++] << 32)
                            | (uint)(Program[Pointer++] << 16)
                            | Program[Pointer++];

                        Stack.Push(new BitsValue(value, Program[Pointer++]));
                        break;
                    }

                case OpCode.Dup:
                    Pointer++;
                    Stack.Push(Stack.Peek());
                    break;

                case OpCode.Show:
                    Pointer++;
                    Console.WriteLine($"Debug print: {Stack.Pop()}");
                    break;
            }

            return Pointer < Program.Length;
        }
    }
}