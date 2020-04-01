using LogicScript.Data;

namespace LogicScript
{
    public interface IMachine
    {
        int InputCount { get; }
        int OutputCount { get; }

        bool GetInput(int i);

        void SetOutput(int i, bool on);
        void SetOutputs(BitsValue values);
    }
}
