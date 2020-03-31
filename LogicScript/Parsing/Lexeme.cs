using System;

namespace LogicScript.Parsing
{
    public enum LexemeKind
    {
        Whitespace,
        NewLine,
        EOF,

        Number,
        Keyword,

        LeftBracket,
        RightBracket,
        LeftParenthesis,
        RightParenthesis,

        Apostrophe,
        Comma,
        Equals
    }

    public readonly struct Lexeme
    {
        public readonly LexemeKind Kind;
        public readonly string? Content;
        public readonly int Line;

        public Lexeme(LexemeKind kind, string? content, int line)
        {
            this.Kind = kind;
            this.Content = content;
            this.Line = line;
        }

        public override string ToString() => Kind.ToString();
    }
}
