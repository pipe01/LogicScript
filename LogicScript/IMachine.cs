using LogicScript.Data;
using LogicScript.Parsing.Structures;
using System;
using System.Collections.Generic;

namespace LogicScript
{
    public readonly record struct MachineRegister(int BitSize, int VectorLength);

    public interface IMachine
    {
        int InputCount { get; }
        int OutputCount { get; }

        BitsValue ReadInputs();
        bool ReadInput(int index);

        void WriteOutputs(int startIndex, BitsValue value);
        void WriteOutput(int index, bool value);

        void AllocateRegisters(MachineRegister[] registers);
        ulong ReadRegister(int index);
        void WriteRegister(int index, ulong value);

        void Print(string msg);

        void QueueUpdate() => throw new NotImplementedException("This machine cannot queue updates");
    }
}
