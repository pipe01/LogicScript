namespace LogicScript.Parsing.Structures
{
    internal enum BinaryOperator
    {
        And,
        Or,
        Xor,
        ShiftLeft,
        ShiftRight,

        AndAlso,
        OrElse,

        Add,
        Subtract,
        Multiply,
        Divide,
        Power,
        Modulus,

        // Comparison operators
        EqualsCompare,
        NotEqualsCompare,
        Greater,
        Lesser,
    }

    internal enum UnaryOperator
    {
        Not,
        Rise,
        Fall,
        Change,
        Length,
        AllOnes,
    }
}
