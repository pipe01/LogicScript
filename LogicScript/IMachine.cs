using LogicScript.Data;
using System;

namespace LogicScript
{
    public interface IMachine
    {
        int InputCount { get; }
        int OutputCount { get; }

        void ReadInputs(Span<bool> values);
        bool ReadInput(int index);

        void WriteOutputs(int startIndex, Span<bool> value);
        void WriteOutput(int index, bool value);

        void AllocateRegisters(int count);
        BitsValue ReadRegister(int index);
        void WriteRegister(int index, BitsValue value);

        void Print(string msg);
    }

    public interface IUpdatableMachine : IMachine
    {
        void QueueUpdate();
    }
}
