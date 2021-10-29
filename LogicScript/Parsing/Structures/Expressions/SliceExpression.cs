﻿namespace LogicScript.Parsing.Structures.Expressions
{
    internal enum IndexStart
    {
        Left,
        Right,
    }

    internal sealed class SliceExpression : Expression
    {
        public Expression Operand { get; set; }
        public IndexStart Start { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }

        public override bool IsConstant => Operand.IsConstant;

        public override int BitSize => Length;

        public SliceExpression(SourceLocation location, Expression operand, IndexStart start, int offset, int length) : base(location)
        {
            this.Operand = operand;
            this.Offset = offset;
            this.Length = length;
            this.Start = start;
        }
    }
}