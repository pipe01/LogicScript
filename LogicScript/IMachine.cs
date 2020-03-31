using System;

namespace LogicScript
{
    public interface IMachine
    {
        bool GetInput(int i);

        void SetOutput(int i, bool on);
        void SetOutputs(Span<bool> values);
    }
}
