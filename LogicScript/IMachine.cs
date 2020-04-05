using LogicScript.Data;

namespace LogicScript
{
    public interface IMachine
    {
        IMemory Memory { get; }

        int InputCount { get; }
        int OutputCount { get; }

        bool GetInput(int i);
        BitsValue GetInputs();

        void SetOutput(int i, bool on);
        void SetOutputs(BitsValue values);
    }
}
