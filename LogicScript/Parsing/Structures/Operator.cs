namespace LogicScript.Parsing.Structures
{
    internal enum Operator
    {
        // Binary operators
        And,
        Or,
        Xor,

        // (Binary) comparison operators
        EqualsCompare,
        Greater,
        Lesser,

        // Unary operators
        Not,
        Rise,
        Fall,
        Change,
    }
}
