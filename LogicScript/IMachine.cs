using System;
using LogicScript.Data;

namespace LogicScript
{
    public interface IMachine
    {
        int InputCount { get; }
        int OutputCount { get; }

        BitsValue ReadInputs(int startIndex, int count);
        bool ReadInput(int index);

        void WriteOutputs(int startIndex, BitsValue value);
        void WriteOutput(int index, bool value);


        void Print(string msg);
        void PrintLine(string msg) { Print(msg + "\n"); }

        void QueueUpdate() => throw new NotImplementedException("This machine cannot queue updates");
    }
}
