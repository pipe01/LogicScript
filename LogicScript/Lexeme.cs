using System;

namespace LogicScript
{
    public enum LexemeKind
    {
        Whitespace,
        NewLine,

        Number,
        Keyword,

        LeftBracket,
        RightBracket,
        LeftParenthesis,
        RightParenthesis,

        Dot,
        Comma,
        Equals
    }

    public readonly struct Lexeme
    {
        public readonly LexemeKind Kind;
        public readonly string Content;
        public readonly int Line;

        public Lexeme(LexemeKind kind, string content, int line)
        {
            this.Kind = kind;
            this.Content = content;
            this.Line = line;
        }

        public override string ToString() => Kind.ToString();
    }
}
