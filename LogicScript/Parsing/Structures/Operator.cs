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
