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
            Program[label.AddressPointer] = (ushort)(CurrentPosition >> 16);
            Program[label.AddressPointer + 1] = (ushort)(CurrentPosition & 0xFFFF);
        }

        private void Jump(OpCode op, ref Label to)
        {
            Push(op);
            to.AddressPointer = NextPosition;
            Push((uint)0);
        }
    }
}