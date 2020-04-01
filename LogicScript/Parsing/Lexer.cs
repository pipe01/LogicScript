using System;
using System.Collections.Generic;
using System.Text;

namespace LogicScript.Parsing
{
    public class Lexer
    {
        private int Index;
        private int Line;
        private int Column;
        private char Current;

        private bool IsEOF => Index == Text.Length;
        private bool IsDigit => char.IsDigit(Current);
        private bool IsLetter => char.IsLetter(Current);
        private bool IsWhitespace => Current == ' ' || Current == '\t';
        private bool IsNewLine => Current == '\n';
        private bool IsComment => Current == '#';

        private readonly StringBuilder Builder = new StringBuilder();
        private readonly string Text;
        private readonly ErrorSink Errors;

        public Lexer(string text, ErrorSink errors)
        {
            this.Text = text?.Replace("\r\n", "\n") ?? throw new ArgumentNullException(nameof(text));
            this.Errors = errors;

            Current = Text[0];
        }

        public IEnumerable<Lexeme> Lex()
        {
            while (TakeLexeme(out var lexeme))
            {
                if (lexeme != null)
                    yield return lexeme.Value;
            }

            if (!IsEOF)
                Errors.AddError(new SourceLocation(Line, Column), $"invalid character found: {Current}");

            yield return Lexeme(LexemeKind.EOF, null);
        }

        private bool Advance()
        {
            Index++;
            Column++;

            if (Index < Text.Length)
                Current = Text[Index];
            else
                return false;

            return true;
        }

        private Lexeme Lexeme(LexemeKind kind, string content) => new Lexeme(kind, content, new SourceLocation(Line, Column - (content?.Length ?? 0)));

        private Lexeme Lexeme(LexemeKind kind) => Lexeme(kind, Builder.ToString());

        private bool TakeLexeme(out Lexeme? lexeme)
        {
            if (IsEOF)
            {
                lexeme = default;
                return false;
            }
            else if (IsDigit)
            {
                lexeme = TakeNumberOrDigit();
            }
            else if (IsLetter)
            {
                lexeme = TakeKeyword();
            }
            else if (IsWhitespace)
            {
                lexeme = Lexeme(LexemeKind.Whitespace, null);
                Advance();
            }
            else if (IsNewLine)
            {
                lexeme = Lexeme(LexemeKind.NewLine, null);
                Advance();

                Line++;
                Column = 0;
            }
            else if (IsComment)
            {
                while (Current != '\n')
                    Advance();

                lexeme = null;
            }
            else
            {
                var (isSymbol, l) = TryTakeSymbol();
                if (isSymbol)
                {
                    lexeme = l;

                    Advance();
                    return true;
                }

                lexeme = default;
                return false;
            }

            return true;
        }

        private Lexeme TakeKeyword()
        {
            Builder.Clear();

            do
            {
                Builder.Append(Current);
            } while (Advance() && IsLetter);

            return Lexeme(LexemeKind.Keyword);
        }

        private Lexeme TakeNumberOrDigit()
        {
            Builder.Clear();

            do
            {
                Builder.Append(Current);
            } while (Advance() && IsDigit);

            return Lexeme(LexemeKind.Number);
        }

        private (bool Success, Lexeme Lexeme) TryTakeSymbol()
        {
            switch (Current)
            {
                case '=':
                    return (true, Lexeme(LexemeKind.Equals));
                case ',':
                    return (true, Lexeme(LexemeKind.Comma));
                case '\'':
                    return (true, Lexeme(LexemeKind.Apostrophe));
                case '[':
                    return (true, Lexeme(LexemeKind.LeftBracket));
                case ']':
                    return (true, Lexeme(LexemeKind.RightBracket));
                case '(':
                    return (true, Lexeme(LexemeKind.LeftParenthesis));
                case ')':
                    return (true, Lexeme(LexemeKind.RightParenthesis));
            }

            return (false, default);
        }
    }
}
