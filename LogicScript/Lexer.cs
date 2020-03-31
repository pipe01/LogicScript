using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LogicScript
{
    public class Lexer
    {
        private int Index;
        private int Line;
        private char Current;

        private bool IsEOF => Index == Text.Length - 1;
        private bool IsDigit => char.IsDigit(Current);
        private bool IsLetter => char.IsLetter(Current);
        private bool IsWhitespace => Current == ' ' || Current == '\t';
        private bool IsNewLine => Current == '\n';

        private readonly StringBuilder Builder = new StringBuilder();
        private readonly string Text;

        public Lexer(string text)
        {
            this.Text = text?.Replace("\r\n", "\n") ?? throw new ArgumentNullException(nameof(text));

            Current = Text[0];
        }

        public IEnumerable<Lexeme> Lex()
        {
            while (!IsEOF && TakeLexeme(out var lexeme))
            {
                yield return lexeme;

                //Advance();
            }

            if (!IsEOF)
                throw new Exception($"Invalid character found: {Current}");
        }

        private bool Advance()
        {
            if (IsEOF)
                return false;

            Index++;
            Current = Text[Index];

            return true;
        }

        private Lexeme Lexeme(LexemeKind kind, string? content) => new Lexeme(kind, content, Line);

        private Lexeme Lexeme(LexemeKind kind) => Lexeme(kind, Builder.ToString());

        private bool TakeLexeme(out Lexeme lexeme)
        {
            if (IsDigit)
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
