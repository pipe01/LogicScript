using LogicScript.Data;
using System;

namespace LogicScript
{
    public interface IMachine
    {
        IMemory Memory { get; }

        int InputCount { get; }
        int OutputCount { get; }

        void GetInputs(BitRange range, Span<bool> inputs);

        void SetOutput(int i, bool on);
        void SetOutputs(BitRange range, Span<bool> values);
    }

    public interface IUpdatableMachine : IMachine
    {
        void QueueUpdate();
    }
}
