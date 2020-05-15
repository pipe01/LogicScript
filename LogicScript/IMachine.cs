using LogicScript.Data;
using System;

namespace LogicScript
{
    public interface IMachine
    {
        IMemory Memory { get; }

        int InputCount { get; }
        int OutputCount { get; }

        BitsValue GetInputs(int start);
        void SetOutputs(int start, BitsValue values);
    }

    public interface IUpdatableMachine : IMachine
    {
        void QueueUpdate();
    }
}
