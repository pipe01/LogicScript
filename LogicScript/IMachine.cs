using LogicScript.Data;
using System;

namespace LogicScript
{
    public interface IMachine
    {
        int InputCount { get; }
        int OutputCount { get; }

        void ReadInput(Span<bool> values);
        void WriteOutput(int startIndex, Span<bool> value);

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
