﻿using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal class BinaryOperatorExpression : Expression
    {
        public Operator Operator { get; set; }
        public Expression Left { get; set; }
        public Expression Right { get; set; }

        public override bool IsConstant => Left.IsConstant && Right.IsConstant;
        public override int BitSize => Operator switch
        {
            Operator.And or Operator.Or or Operator.Xor or Operator.Subtract or Operator.Divide => Left.BitSize > Right.BitSize ? Left.BitSize : Right.BitSize,
            Operator.ShiftLeft => Left.BitSize + ((1 << Right.BitSize) - 1),
            Operator.ShiftRight => Left.BitSize - Right.BitSize,
            Operator.EqualsCompare or Operator.NotEqualsCompare or Operator.Greater or Operator.Lesser => 1,
            Operator.Add => Left.BitSize > Right.BitSize ? Left.BitSize + 1 : Right.BitSize + 1,
            Operator.Multiply => Left.BitSize + Right.BitSize,
            Operator.Power => Left.BitSize * ((1 << Right.BitSize) - 1),
            Operator.Modulus => Right.BitSize,
            _ => throw new ParseException("Unknown operator bitsize", Span)
        };

        public BinaryOperatorExpression(SourceSpan span, Operator op, Expression left, Expression right) : base(span)
        {
            this.Operator = op;
            this.Left = left;
            this.Right = right;
        }

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Left;
            yield return Right;
        }

        public override string ToString() => $"{Operator}({Left}, {Right})";
    }
}
