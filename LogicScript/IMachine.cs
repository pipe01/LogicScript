using System;

namespace LogicScript
{
    public interface IMachine
    {
        bool GetInput(int i);
        Memory<bool> GetInputs();

        void SetOutput(int i, bool on);
        void SetOutputs(Span<bool> values);
    }
}
