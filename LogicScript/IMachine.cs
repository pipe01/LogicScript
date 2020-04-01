using LogicScript.Data;
using System;

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
