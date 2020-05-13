using LogicScript.Data;
using System;

namespace LogicScript
{
    public interface IMachine
    {
        IMemory Memory { get; }

        int InputCount { get; }
        int OutputCount { get; }

        void GetInputs(int start, Span<bool> inputs);
        void SetOutputs(int start, Span<bool> values);
    }

    public interface IUpdatableMachine : IMachine
    {
        void QueueUpdate();
    }
}
