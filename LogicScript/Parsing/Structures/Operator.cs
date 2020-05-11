namespace LogicScript.Parsing.Structures
{
    internal enum Operator
    {
        None = -1,

        Assign,

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

        Modulo,
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
