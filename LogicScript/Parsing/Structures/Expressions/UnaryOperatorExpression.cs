using System;
using System.Collections.Generic;
using LogicScript.Data;
using LogicScript.Interpreting;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class UnaryOperatorExpression(SourceSpan span, UnaryOperator op, Expression operand) : Expression(span)
    {
        public UnaryOperator Operator { get; set; } = op;
        public Expression Operand { get; set; } = operand;

        public override bool IsConstant => Operator == UnaryOperator.Length || Operand.IsConstant;
        public override int BitSize => Operator switch
        {
            UnaryOperator.Not or UnaryOperator.Rise or UnaryOperator.Fall or UnaryOperator.Change => Operand.BitSize,
            UnaryOperator.Length => 7,
            UnaryOperator.AllOnes => 1,
            _ => throw new ParseException("Unknown unary operator bitsize", Span)
        };

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Operand;
        }

        public override string ToString() => $"{Operator}({Operand})";

        public override BitsValue GetValue(in GetValueContext ctx)
        {
            if (Operator == UnaryOperator.Length)
                return new BitsValue((ulong)Operand.BitSize, 7);

            var operand = Operand.GetValue(ctx);

            return Operator switch
            {
                UnaryOperator.Not => operand.Negated,
                UnaryOperator.Rise => throw new NotImplementedException(),
                UnaryOperator.Fall => throw new NotImplementedException(),
                UnaryOperator.Change => throw new NotImplementedException(),
                UnaryOperator.AllOnes => (BitsValue)operand.AreAllBitsSet,
                _ => throw new InterpreterException("Unknown operand", Span),
            };
        }
    }
}
