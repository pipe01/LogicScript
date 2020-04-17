using LogicScript.Parsing.Structures;
using LogicScript.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LogicScript.Parsing
{
    internal class Lexer : IDisposable
    {
        private int Line;
        private int Column;
        private char Current => Reader.Current;

        private bool IsEOF => Reader.IsEOF;
        private bool IsDigit => char.IsDigit(Current);
        private bool IsLetter => char.IsLetter(Current);
        private bool IsWhitespace => Current == ' ' || Current == '\t';
        private bool IsNewLine => Current == '\n';
        private bool IsComment => Current == '#';

        private SourceLocation Location => new SourceLocation(Line, Column);

        private readonly StringBuilder Builder = new StringBuilder();
        private readonly ErrorSink Errors;
        private readonly CharReader Reader;

        public Lexer(Stream stream, ErrorSink errors)
        {
            this.Reader = new CharReader(stream);
            this.Errors = errors;
        }

        public Lexer(string text, ErrorSink errors) : this(new MemoryStream(Encoding.UTF8.GetBytes(text)), errors)
        {
        }

        public void Dispose()
        {
            Reader.Dispose();
        }

        public IEnumerable<Lexeme> Lex()
        {
            while (TakeLexeme(out var lexeme))
            {
                if (lexeme != null)
                    yield return lexeme.Value;
            }

            if (!IsEOF)
                Errors.AddError(Location, $"invalid character found: {Current}");

            yield return Lexeme(LexemeKind.EOF, null);
        }

        private bool Advance()
        {
            Column++;

            return Reader.TryAdvance();
        }

        private bool Take(char c)
        {
            if (Reader.TryPeek(out var peek) && peek == c)
            {
                Advance();
                return true;
            }

            return false;
        }

        private char Consume()
        {
            var c = Current;
            Advance();
            return c;
        }

        private Lexeme Lexeme(LexemeKind kind, string content) => new Lexeme(kind, content, new SourceLocation(Line, Column - (content?.Length ?? 0)));

        private Lexeme Lexeme(LexemeKind kind)
        {
            var content = Builder.ToString();
            Builder.Clear();

            return Lexeme(kind, content);
        }

        private bool TakeLexeme(out Lexeme? lexeme)
        {
            if (Current == '\r') //Skip \r chars in order to not emit two NewLine lexemes on \r\n sequences
            {
                lexeme = default;
                Advance();
            }
            else if (IsEOF)
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
                while (!IsNewLine)
                    Advance();

                lexeme = default;
            }
            else if (TryTakeOperator(out var opLex))
            {
                lexeme = opLex;
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

        private bool TryTakeOperator(out Lexeme lexeme)
        {
            LexemeKind kind;

            switch (Current)
            {
                case '+':
                    kind = LexemeKind.Add;
                    break;
                case '-':
                    kind = LexemeKind.Subtract;
                    break;
                case '*':
                    kind = LexemeKind.Multiply;
                    break;
                case '/':
                    kind = LexemeKind.Divide;
                    break;
                case '%':
                    kind = LexemeKind.Modulo;
                    break;
                case '=':
                    kind = Take('=') ? LexemeKind.Equals : LexemeKind.EqualsAssign;
                    break;
                case '!':
                    kind = Take('=') ? LexemeKind.NotEquals : LexemeKind.Not;
                    break;
                case '>':
                    kind = Take('=') ? LexemeKind.GreaterOrEqual :
                           Take('>') ? LexemeKind.BitShiftRight : LexemeKind.Greater;
                    break;
                case '<':
                    kind = Take('=') ? LexemeKind.LesserOrEqual :
                           Take('<') ? LexemeKind.BitShiftLeft : LexemeKind.Lesser;
                    break;
                case '&':
                    kind = LexemeKind.And;
                    break;
                case '|':
                    kind = LexemeKind.Or;
                    break;
                case '^':
                    kind = LexemeKind.Xor;
                    break;

                default:
                    lexeme = default;
                    return false;
            }

            lexeme = Lexeme(kind);
            Advance();
            return true;
        }

        private Lexeme TakeKeyword()
        {
            var startLocation = Location;

            Builder.Clear();

            do
            {
                Builder.Append(Current);
            } while (Advance() && IsLetter);

            string keyword = Builder.ToString();

            if (!Constants.Keywords.Contains(keyword))
                Errors.AddError(startLocation, "invalid keyword");

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
                    return (true, Lexeme(LexemeKind.EqualsAssign));
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
