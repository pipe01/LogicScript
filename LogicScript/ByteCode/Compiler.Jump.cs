using System;
using System.Collections.Generic;

namespace LogicScript.ByteCode
{
    partial struct Compiler
    {
        private class Label
        {
            public IList<int> ReplaceAt { get; } = new List<int>();

            public bool IsLoop { get; }
            public bool IsMarked { get; set; }
            public int Pointer { get; set; }

            public Label(bool isLoop)
            {
                this.IsLoop = isLoop;
            }
            public Label(bool isLoop, int pointer) : this(isLoop)
            {
                this.Pointer = pointer;
                this.IsMarked = true;
            }
        }

        private Label NewLabel(bool isLoop = false)
        {
            var label = new Label(isLoop);

            if (isLoop)
                LoopStack.Push(label);

            return label;
        }

        private Label NewLabel(int pointer, bool isLoop = false)
        {
            var label = new Label(isLoop, pointer);

            if (isLoop)
                LoopStack.Push(label);

            return label;
        }

        private void MarkLabel(Label label)
        {
            if (label.IsMarked)
                return;

            label.Pointer = CurrentPosition;
            label.IsMarked = true;

            foreach (var item in label.ReplaceAt)
            {
                Program[item] = (byte)(CurrentPosition >> 24);
                Program[item + 1] = (byte)((CurrentPosition >> 16) & 0xFF);
                Program[item + 2] = (byte)((CurrentPosition >> 8) & 0xFF);
                Program[item + 3] = (byte)(CurrentPosition & 0xFF);
            }

            if (label.IsLoop)
                LoopStack.Pop();
        }

        private void Jump(OpCodes op, Label to)
        {
            Push(op);

            if (to.IsMarked)
            {
                Push(to.Pointer);
            }
            else
            {
                to.ReplaceAt.Add(NextPosition);

                Push((uint)0);
            }
        }

        private void Jump(OpCodes op, int to)
        {
            Push(op);
            Push(to);
        }
    }
}