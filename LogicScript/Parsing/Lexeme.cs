using System;

namespace LogicScript.Parsing
{
    internal enum LexemeKind
    {
        Whitespace,
        NewLine,
        EOF,
        AtSign,

        Number,
        Keyword,
        String,

        LeftBracket,
        RightBracket,
        LeftParenthesis,
        RightParenthesis,

        Apostrophe,
        Comma,
        DotDot,
        Hat,
        EqualsAssign,

        //Operators
        //=========
        Not,

        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,

        Truncate,

        Equals,
        NotEquals,
        Greater,
        GreaterOrEqual,
        Lesser,
        LesserOrEqual,

        And,
        Or,

        BitShiftLeft,
        BitShiftRight,
    }

    internal readonly struct Lexeme
    {
        public readonly LexemeKind Kind;
        public readonly string Content;
        public readonly SourceLocation Location;

        public Lexeme(LexemeKind kind, string content, SourceLocation location)
        {
            this.Kind = kind;
            this.Content = content;
            this.Location = location;
        }

        public override string ToString() => $"{Kind} ({Content})";
    }
}
