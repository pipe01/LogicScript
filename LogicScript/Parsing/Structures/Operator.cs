namespace LogicScript.Parsing.Structures
{
    internal enum Operator
    {
        // Binary operators
        And,
        Or,
        Xor,
        Add,
        Subtract,
        Multiply,
        Divide,

        // (Binary) comparison operators
        EqualsCompare,
        Greater,
        Lesser,

        // Unary operators
        Not,
        Rise,
        Fall,
        Change,
        Length,
    }
}
