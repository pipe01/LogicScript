using LogicScript.Data;
using System;

namespace LogicScript.Interpreting
{
    public interface IMachine
    {
        int InputCount { get; }
        int OutputCount { get; }

        void ReadInput(Span<bool> values);
        void WriteOutput(int startIndex, BitsValue value);

        void AllocateRegisters(int count);
        BitsValue ReadRegister(int index);
        void WriteRegister(int index, BitsValue value);

        void Print(string msg);
    }
}
