using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogicScript.Data;

namespace LogicScript.ByteCode
{
    public ref struct CPU
    {
        private TapeReader Tape;
        private Stack<BitsValue> Stack = new();

        public CPU(byte[] program)
        {
            this.Tape = new TapeReader(program);
        }

        public void Run()
        {
            while (!Tape.IsEOF)
                ProcessInstruction();

            if (Stack.Count != 0)
            {
                throw new Exception($"Stack wasn't empty when program ended");
            }
        }

        private void ProcessInstruction()
        {
            var opcode = Tape.ReadOpCode();

            switch (opcode)
            {
                case OpCode.Nop:
                    break;

                case OpCode.Pop:
                    Stack.Pop();
                    break;

                case OpCode.Ld_0:
                    Stack.Push(new BitsValue(0, Tape.ReadByte()));
                    break;

                case OpCode.Ld_1:
                    Stack.Push(new BitsValue(1, Tape.ReadByte()));
                    break;

                case OpCode.Ld_0_1:
                    Stack.Push(BitsValue.Zero);
                    break;

                case OpCode.Ld_1_1:
                    Stack.Push(BitsValue.One);
                    break;

                case OpCode.Ldi_8:
                    Stack.Push(new BitsValue(Tape.ReadByte(), Tape.ReadByte()));
                    break;

                case OpCode.Ldi_16:
                    Stack.Push(new BitsValue(Tape.ReadUInt16(), Tape.ReadByte()));
                    break;

                case OpCode.Ldi_32:
                    Stack.Push(new BitsValue(Tape.ReadUInt32(), Tape.ReadByte()));
                    break;

                case OpCode.Ldi_64:
                    Stack.Push(new BitsValue(Tape.ReadUInt64(), Tape.ReadByte()));
                    break;

                case OpCode.Dup:
                    Stack.Push(Stack.Peek());
                    break;

                case OpCode.Show:
                    Console.WriteLine($"Debug print: {Stack.Pop()}");
                    break;

                case OpCode.Jmp:
                    Tape.JumpToAddress();
                    break;

                case OpCode.Brz or OpCode.Brnz:
                    if ((Stack.Pop() == 0) == (opcode == OpCode.Brz))
                        Tape.JumpToAddress();
                    else
                        Tape.ReadAddress();

                    break;

                case OpCode.Breq or OpCode.Brneq:
                    if ((Stack.Pop() == Stack.Pop()) == (opcode == OpCode.Breq))
                        Tape.JumpToAddress();
                    else
                        Tape.ReadAddress();

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
        }
    }
}