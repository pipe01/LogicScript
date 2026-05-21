using LogicScript.Data;
using System;

namespace LogicScript
{
    public interface IMachine
    {
        int InputCount { get; }
        int OutputCount { get; }

        BitsValue ReadInputs();
        bool ReadInput(int index);

        void WriteOutputs(int startIndex, BitsValue value);
        void WriteOutput(int index, bool value);

        void AllocateRegisters(int count);
        ulong ReadRegister(int index);
        void WriteRegister(int index, ulong value);

        void Print(string msg);

        void QueueUpdate() => throw new NotImplementedException("This machine cannot queue updates");
    }
}
