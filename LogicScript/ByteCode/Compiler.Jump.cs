using System;

namespace LogicScript.ByteCode
{
    partial struct Compiler
    {
        private ref struct Label
        {
            public int AddressPointer;

            public Label()
            {
                this.AddressPointer = 0;
            }
        }

        private Label NewLabel() => new Label();

        private void MarkLabel(Label label)
        {
            Program[label.AddressPointer] = (byte)(CurrentPosition >> 24);
            Program[label.AddressPointer + 1] = (byte)((CurrentPosition >> 16) & 0xFF);
            Program[label.AddressPointer + 2] = (byte)((CurrentPosition >> 8) & 0xFF);
            Program[label.AddressPointer + 3] = (byte)(CurrentPosition & 0xFF);
        }

        private void Jump(OpCode op, ref Label to)
        {
            Push(op);
            to.AddressPointer = NextPosition;
            Push((uint)0);
        }
    }
}