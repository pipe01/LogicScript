using LogicScript.Data;

namespace LogicScript
{
    public interface IMachine
    {
        IMemory Memory { get; }

        int InputCount { get; }
        int OutputCount { get; }

        BitsValue GetInputs(int start, int count);
        void SetOutputs(int start, BitsValue values);
    }

    public interface IUpdatableMachine : IMachine
    {
        void QueueUpdate();
    }
}
