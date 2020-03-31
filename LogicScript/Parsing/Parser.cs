using LogicScript.Parsing.Structures;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace LogicScript.Parsing
{
    internal class Parser
    {
        private int Index;
        private ref Lexeme Current => ref Lexemes[Index];

        private readonly Lexeme[] Lexemes;

        public Parser(Lexeme[] lexemes)
        {
            this.Lexemes = lexemes;
        }

        private bool Advance()
        {
            if (Index == Lexemes.Length - 1)
                return false;

            Index++;
            return true;
        }

        private bool TakeKeyword(string keyword, bool @throw = true)
        {
            if (Current.Kind != LexemeKind.Keyword || Current.Content != keyword)
            {
                if (@throw)
                    throw new Exception();

                return false;
            }

            Advance();
            return true;
        }

        private bool Take(LexemeKind kind, bool @throw = true)
        {
            if (Current.Kind != kind)
            {
                if (@throw)
                    throw new Exception();

                return false;
            }

            Advance();
            return true;
        }

        private bool Take(LexemeKind kind, out Lexeme lexeme, bool @throw = true, string? expected = null)
        {
            if (Current.Kind != kind)
            {
                if (@throw)
                    throw new Exception($"Expected {expected ?? kind.ToString()}, found {Current.Kind}");

                lexeme = default;
                return false;
            }

            lexeme = Current;
            Advance();
            return true;
        }

        private void SkipWhitespaces(bool newlines = false)
        {
            while (Current.Kind == LexemeKind.Whitespace || (Current.Kind == LexemeKind.NewLine && newlines))
            {
                Advance();
            }
        }

        public Case TakeCase()
        {
            TakeKeyword("when");
            SkipWhitespaces();

            var inputSpec = TakeInputSpec();

            SkipWhitespaces();
            Take(LexemeKind.Equals);
            SkipWhitespaces();

            var inputValSpec = TakeInputValSpec();

            SkipWhitespaces();
            Take(LexemeKind.NewLine);

            return null;
        }

        private Statement TakeStatement()
        {
            SkipWhitespaces();


        }

        private InputSpec TakeInputSpec()
        {
            if (TakeKeyword("in", @throw: false))
                return new WholeInputSpec();

            Take(LexemeKind.LeftParenthesis);

            var inputs = new List<int>();
            do
            {
                SkipWhitespaces();
                inputs.Add(TakeInput());
            } while (Take(LexemeKind.Comma, false));

            Take(LexemeKind.RightParenthesis);

            return new CompoundInputSpec(inputs.ToArray());
        }

        private InputValSpec TakeInputValSpec()
        {
            return new InputValSpec(TakeBitValue());
        }

        private BitValue TakeBitValue()
        {
            if (Take(LexemeKind.LeftParenthesis, @throw: false))
            {
                var values = new List<bool>();

                do
                {
                    SkipWhitespaces();
                    Take(LexemeKind.Number, out var n);

                    if (n.Content?.Length != 1 || (n.Content != "1" && n.Content != "0"))
                        throw new Exception("Expected bit value (0 or 1)");

                    values.Add(n.Content == "1");

                } while (Take(LexemeKind.Comma, false));

                Take(LexemeKind.RightParenthesis);

                return new LiteralBitValue(values.ToArray());
            }
            else
            {
                Take(LexemeKind.Number, out var n);

                int @base = 2;
                if (Take(LexemeKind.Apostrophe, false))
                    @base = 10;

                return new LiteralBitValue(Convert.ToUInt64(n.Content, @base));
            }
        }

        private int TakeInput()
        {
            TakeKeyword("in");
            Take(LexemeKind.LeftBracket);
            Take(LexemeKind.Number, out var numLexeme);
            Take(LexemeKind.RightBracket);

            return int.Parse(numLexeme.Content);
        }
    }
}
