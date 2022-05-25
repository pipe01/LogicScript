using System;
using System.Collections.Generic;
using LogicScript.Data;

namespace LogicScript.ByteCode
{
    public ref struct CPU
    {
        private readonly TapeReader Tape;
        private readonly Header Header;
        private readonly Stack<BitsValue> Stack = new();
        private readonly IMachine Machine;

        private BitsValue[] Locals;

        public CPU(byte[] program, IMachine machine)
        {
            this.Tape = new TapeReader(program);
            this.Machine = machine;
            this.Header = Tape.ReadHeader();

            this.Locals = new BitsValue[Header.LocalsCount];
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

                case OpCode.Ldloc:
                    Stack.Push(Locals[Tape.ReadByte()]);
                    break;

                case OpCode.Stloc:
                    Locals[Tape.ReadByte()] = Stack.Pop();
                    break;

                default:
                    throw new Exception("Invalid instruction");
            }
        }
    }
}