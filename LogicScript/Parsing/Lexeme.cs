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
        Equals,
        Hash,
    }

    public readonly struct Lexeme
    {
        public readonly LexemeKind Kind;
        public readonly string? Content;
        public readonly SourceLocation Location;

        public Lexeme(LexemeKind kind, string? content, SourceLocation location)
        {
            this.Kind = kind;
            this.Content = content;
            this.Location = location;
        }

        public override string ToString() => Kind.ToString();
    }
}
