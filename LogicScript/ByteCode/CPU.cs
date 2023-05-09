using System;
using System.Collections.Generic;
using System.Linq;
using LogicScript.Data;
using LogicScript.Interpreting;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.ByteCode
{
    public class CPU
    {
        private const int MaxStackSize = 100;

        private readonly TapeReader Tape;
        private readonly Header Header;
        private readonly BitsValue[] Stack = new BitsValue[MaxStackSize];
        private readonly IMachine Machine;

        private readonly bool[] OutputBuffer = new bool[BitsValue.BitSize];
        private readonly bool[] InputBuffer;

        /// <summary>
        /// Points to the lowest empty stack slot.
        /// </summary>
        private int StackPointer = 0;
        private BitsValue[] Locals;

        public CPU(byte[] program, IMachine machine)
        {
            this.Tape = new TapeReader(program);
            this.Machine = machine;
            this.Header = Tape.ReadHeader();

            this.Locals = new BitsValue[Header.LocalsCount];
            this.InputBuffer = new bool[machine.InputCount];

            machine.AllocateRegisters(Header.RegisterCount);
        }

        public void Run(bool reset)
        {
            if (reset)
                Tape.Position = Header.Size;

            Machine.ReadInput(InputBuffer);

            bool yield = false;
            while (!yield)
            {
                ProcessInstruction(ref yield);
            }

            if (StackPointer != 0)
                throw new Exception("Stack wasn't empty when program ended");
        }

        private void Push(BitsValue v) => Stack[StackPointer++] = v;
        private BitsValue Pop() => Stack[--StackPointer];
        private BitsValue Peek() => Stack[StackPointer - 1];

        private void ProcessInstruction(ref bool yield)
        {
            var opcode = Tape.ReadOpCode();
            yield = false;

            switch (opcode)
            {
                case OpCodes.Nop:
                    break;

                case OpCodes.Pop:
                    Pop();
                    break;

                case OpCodes.Ld_0:
                    Push(new BitsValue(0, Tape.ReadByte()));
                    break;

                case OpCodes.Ld_1:
                    Push(new BitsValue(1, Tape.ReadByte()));
                    break;

                case OpCodes.Ld_0_1:
                    Push(BitsValue.Zero);
                    break;

                case OpCodes.Ld_1_1:
                    Push(BitsValue.One);
                    break;

                case OpCodes.Ldi_8:
                    Push(new BitsValue(Tape.ReadByte(), Tape.ReadByte()));
                    break;

                case OpCodes.Ldi_16:
                    Push(new BitsValue(Tape.ReadUInt16(), Tape.ReadByte()));
                    break;

                case OpCodes.Ldi_32:
                    Push(new BitsValue(Tape.ReadUInt32(), Tape.ReadByte()));
                    break;

                case OpCodes.Ldi_64:
                    Push(new BitsValue(Tape.ReadUInt64(), Tape.ReadByte()));
                    break;

                case OpCodes.Dup:
                    Push(Peek());
                    break;

                case OpCodes.Show:
                    Machine.Print(Pop().ToString());
                    break;

                case OpCodes.LoadPortInput:
                    Push(new BitsValue(InputBuffer.AsSpan().Slice(Tape.ReadByte(), Tape.ReadByte())));
                    break;

                case OpCodes.LoadPortRegister:
                    Push(Machine.ReadRegister(Tape.ReadByte()));
                    break;

                case OpCodes.Jmp:
                    Tape.JumpToAddress();
                    break;

                case OpCodes.Brz or OpCodes.Brnz:
                    if ((Pop() == 0) == (opcode == OpCodes.Brz))
                        Tape.JumpToAddress();
                    else
                        Tape.ReadAddress();

                    break;

                case OpCodes.Breq or OpCodes.Brneq:
                    if ((Pop() == Pop()) == (opcode == OpCodes.Breq))
                        Tape.JumpToAddress();
                    else
                        Tape.ReadAddress();

                    break;

                case >= OpCodes.FirstBinOp and <= OpCodes.LastBinOp:
                    Operator op = (Operator)(opcode - OpCodes.FirstBinOp);
                    Push(Operations.DoOperation(Pop(), Pop(), op));
                    break;

                case OpCodes.Not:
                    Push(Pop().Negated);
                    break;

                case OpCodes.Length:
                    Push(Pop().Length);
                    break;

                case OpCodes.AllOnes:
                    Push(Pop().AreAllBitsSet);
                    break;

                case OpCodes.SliceLeft or OpCodes.SliceRight:
                    var start = opcode == OpCodes.SliceLeft ? IndexStart.Left : IndexStart.Right;
                    Push(Operations.Slice(Pop(), start, (int)Pop().Number, Tape.ReadByte()));
                    break;

                case OpCodes.Trunc:
                    Push(Pop().Resize(Tape.ReadByte()));
                    break;

                case OpCodes.Ldloc:
                    Push(Locals[Tape.ReadByte()]);
                    break;

                case OpCodes.Stloc:
                    Locals[Tape.ReadByte()] = Pop();
                    break;

                case OpCodes.Stout:
                    int startIndex = Tape.ReadByte();
                    var value = Pop();
                    value.FillBits(OutputBuffer);

                    Machine.WriteOutput(startIndex, OutputBuffer[..value.Length]);
                    break;

                case OpCodes.Yield:
                    yield = true;
                    break;

                default:
                    throw new Exception("Invalid instruction");
            }
        }
    }
}