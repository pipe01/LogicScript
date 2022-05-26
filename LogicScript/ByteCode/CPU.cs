using System;
using System.Collections.Generic;
using LogicScript.Data;
using LogicScript.Interpreting;
using LogicScript.Parsing.Structures;

namespace LogicScript.ByteCode
{
    public class CPU
    {
        private const int MaxStackSize = 100;

        private readonly TapeReader Tape;
        private readonly Header Header;
        private readonly BitsValue[] Stack = new BitsValue[MaxStackSize];
        private readonly IMachine Machine;

        private int StackPointer = -1;
        private BitsValue[] Locals;

        public CPU(byte[] program, IMachine machine)
        {
            this.Tape = new TapeReader(program);
            this.Machine = machine;
            this.Header = Tape.ReadHeader();

            this.Locals = new BitsValue[Header.LocalsCount];
        }

        public void Run(bool singleTick)
        {
            if (!singleTick)
                Tape.Position = Header.Size;

            bool yield = false;
            while (!yield)
            {
                ProcessInstruction(ref yield);
            }

            if (!singleTick && StackPointer != -1)
                throw new Exception("Stack wasn't empty when program ended");
        }

        private void Push(BitsValue v) => Stack[++StackPointer] = v;
        private BitsValue Pop() => Stack[StackPointer--];
        private BitsValue Peek() => Stack[StackPointer];

        private void ProcessInstruction(ref bool yield)
        {
            var opcode = Tape.ReadOpCode();
            yield = false;

            switch (opcode)
            {
                case OpCode.Nop:
                    break;

                case OpCode.Pop:
                    Pop();
                    break;

                case OpCode.Ld_0:
                    Push(new BitsValue(0, Tape.ReadByte()));
                    break;

                case OpCode.Ld_1:
                    Push(new BitsValue(1, Tape.ReadByte()));
                    break;

                case OpCode.Ld_0_1:
                    Push(BitsValue.Zero);
                    break;

                case OpCode.Ld_1_1:
                    Push(BitsValue.One);
                    break;

                case OpCode.Ldi_8:
                    Push(new BitsValue(Tape.ReadByte(), Tape.ReadByte()));
                    break;

                case OpCode.Ldi_16:
                    Push(new BitsValue(Tape.ReadUInt16(), Tape.ReadByte()));
                    break;

                case OpCode.Ldi_32:
                    Push(new BitsValue(Tape.ReadUInt32(), Tape.ReadByte()));
                    break;

                case OpCode.Ldi_64:
                    Push(new BitsValue(Tape.ReadUInt64(), Tape.ReadByte()));
                    break;

                case OpCode.Dup:
                    Push(Peek());
                    break;

                case OpCode.Show:
                    Machine.Print(Pop().ToString());
                    break;

                case OpCode.Jmp:
                    Tape.JumpToAddress();
                    break;

                case OpCode.Brz or OpCode.Brnz:
                    if ((Pop() == 0) == (opcode == OpCode.Brz))
                        Tape.JumpToAddress();
                    else
                        Tape.ReadAddress();

                    break;

                case OpCode.Breq or OpCode.Brneq:
                    if ((Pop() == Pop()) == (opcode == OpCode.Breq))
                        Tape.JumpToAddress();
                    else
                        Tape.ReadAddress();

                    break;

                case OpCode.Trunc:
                    Push(Pop().Resize(Tape.ReadByte()));
                    break;

                case OpCode.Ldloc:
                    Push(Locals[Tape.ReadByte()]);
                    break;

                case OpCode.Stloc:
                    Locals[Tape.ReadByte()] = Pop();
                    break;

                case >= OpCode.FirstBinOp and <= OpCode.LastBinOp:
                    Operator op = (Operator)(opcode - OpCode.FirstBinOp);
                    Push(Operations.DoOperation(Pop(), Pop(), op));
                    break;

                case OpCode.Not:
                    Push(Pop().Negated);
                    break;

                case OpCode.Length:
                    Push(Pop().Length);
                    break;

                case OpCode.AllOnes:
                    Push(Pop().AreAllBitsSet);
                    break;

                case OpCode.Yield:
                    yield = true;
                    break;

                default:
                    throw new Exception("Invalid instruction");
            }
        }
    }
}