using System;
using System.Collections.Generic;

namespace LogicScript.ByteCode
{
    partial struct Compiler
    {
        private class Label
        {
            public IList<int> ReplaceAt { get; } = new List<int>();
        }

        private Label NewLabel(bool isLoop = false)
        {
            var label = new Label();

            if (isLoop)
                LoopStack.Push(label);

            return label;
        }

        private void MarkLabel(Label label)
        {
            foreach (var item in label.ReplaceAt)
            {
                Program[item] = (byte)(CurrentPosition >> 24);
                Program[item + 1] = (byte)((CurrentPosition >> 16) & 0xFF);
                Program[item + 2] = (byte)((CurrentPosition >> 8) & 0xFF);
                Program[item + 3] = (byte)(CurrentPosition & 0xFF);
            }

            LoopStack.Pop();
        }

        private void Jump(OpCode op, Label to)
        {
            Push(op);
            to.ReplaceAt.Add(NextPosition);
            Push((uint)0);
        }

        private void Jump(OpCode op, int to)
        {
            Push(op);
            Push(to);
        }
    }
}