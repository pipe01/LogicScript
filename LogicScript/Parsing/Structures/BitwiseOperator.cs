namespace LogicScript.Parsing.Structures
{
    internal class BitwiseOperator : Expression
    {
        public override bool IsSingleBit { get; }

        public Operator Operator { get; }
        public Expression[] Operands { get; }

        public BitwiseOperator(Operator @operator, Expression[] operands, SourceLocation location) : base(location)
        {
            this.Operator = @operator;
            this.Operands = operands;

            this.IsSingleBit = true;

            if (operands.Length > 1)
            {
                foreach (var item in operands)
                {
                    if (!item.IsSingleBit)
                    {
                        this.IsSingleBit = false;
                        break;
                    }
                }
            }
        }

        public override string ToString() => $"{Operator}({string.Join<Expression>(", ", Operands)})";
    }

    internal enum Operator
    {
        Add,
        Substract,

        And,
        Or
    }
}
