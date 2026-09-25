namespace LogicScript.Parsing.Structures
{
    internal enum Operator
    {
        // Binary operators
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

        // (Binary) comparison operators
        EqualsCompare,
        NotEqualsCompare,
        Greater,
        Lesser,

        // Unary operators
        Not,
        Rise,
        Fall,
        Change,
        Length,
        AllOnes,
    }
}
