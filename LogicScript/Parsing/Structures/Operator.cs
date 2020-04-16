namespace LogicScript.Parsing.Structures
{
    internal enum Operator
    {
        None = -1,

        NotEquals,
        Equals,
        Greater,
        GreaterOrEqual,
        Lesser,
        LesserOrEqual,

        BitShiftLeft,
        BitShiftRight,

        Add,
        Subtract,

        Multiply,
        Divide,

        And,
        Or,
        Xor,

        //Operators without precedence
        Not,
        Truncate,
    }
}
