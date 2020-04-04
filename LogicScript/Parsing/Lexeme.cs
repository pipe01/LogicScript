using System;

namespace LogicScript.Parsing
{
    internal enum LexemeKind
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
        Operator,
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
