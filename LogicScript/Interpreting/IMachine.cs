using LogicScript.Data;
using System;

namespace LogicScript.Interpreting
{
    public interface IMachine
    {
        int InputCount { get; }
        int OutputCount { get; }

        void ReadInput(Span<bool> values);
        void WriteOutput(int index, bool value);

        BitsValue ReadRegister(int index);
        void WriteRegister(int index, BitsValue value);

        void Print(string msg);
    }
}
