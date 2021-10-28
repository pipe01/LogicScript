using System;

namespace LogicScript.Interpreting
{
    public interface IMachine
    {
        int InputCount { get; }
        int OutputCount { get; }

        void ReadInput(Span<bool> values);
        void WriteOutput(Span<bool> values);

        void Print(string msg);
    }
}
