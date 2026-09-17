using System;
using LogicScript.Data;

namespace LogicScript
{
    public readonly record struct MachineRegister(int BitSize, int VectorLength, int Index);

    public interface IMachine
    {
        int InputCount { get; }
        int OutputCount { get; }

        BitsValue ReadInputs();
        bool ReadInput(int index);

        void WriteOutputs(int startIndex, BitsValue value);
        void WriteOutput(int index, bool value);


        void Print(string msg);

        void QueueUpdate() => throw new NotImplementedException("This machine cannot queue updates");
    }
}
