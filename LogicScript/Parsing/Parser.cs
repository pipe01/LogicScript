using LogicScript.Parsing.Structures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        [DebuggerStepThrough]
        private bool Advance()
        {
            if (Index == Lexemes.Length - 1)
                return false;

            Index++;
            return true;
        }

        [DebuggerStepThrough]
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

        [DebuggerStepThrough]
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

        [DebuggerStepThrough]
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

        [DebuggerStepThrough]
        private void SkipWhitespaces(bool newlines = false)
        {
            while (Current.Kind == LexemeKind.Whitespace || (Current.Kind == LexemeKind.NewLine && newlines))
            {
                if (!Advance())
                    return;
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

            var inputsValue = TakeBitsValue();

            SkipWhitespaces();
            Take(LexemeKind.NewLine);

            var stmts = new List<Statement>();

            do
            {
                stmts.Add(TakeStatement());
            } while (!TakeKeyword("end", false));

            return new Case(inputSpec, inputsValue, stmts.ToArray());
        }

        private Statement TakeStatement()
        {
            SkipWhitespaces();

            var output = TakeOutput();

            SkipWhitespaces();
            Take(LexemeKind.Equals);
            SkipWhitespaces();

            var value = TakeBitsValue();

            Take(LexemeKind.NewLine);

            return new OutputSetStatement(output, value);
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

        private BitsValue TakeBitsValue()
        {
            if (Take(LexemeKind.LeftParenthesis, @throw: false))
            {
                var values = new List<BitValue>();

                do
                {
                    SkipWhitespaces();
                    values.Add(TakeBitValue());
                } while (Take(LexemeKind.Comma, false));

                Take(LexemeKind.RightParenthesis);

                return new CompoundBitsValue(values.ToArray());
            }
            else
            {
                Take(LexemeKind.Number, out var n);

                int @base = 2;
                if (Take(LexemeKind.Apostrophe, false))
                    @base = 10;

                return new LiteralBitsValue(Convert.ToUInt32(n.Content, @base));
            }
        }

        private BitValue TakeBitValue()
        {
            if (Take(LexemeKind.Number, out var n, false))
            {
                if (n.Content?.Length != 1 || (n.Content != "1" && n.Content != "0"))
                    throw new Exception("Expected bit value (0 or 1)");

                return new LiteralBitValue(n.Content == "1");
            }
            else
            {
                return new InputBitValue(TakeInput());
            }
        }

        private Output TakeOutput()
        {
            TakeKeyword("out");

            if (Take(LexemeKind.LeftBracket, false))
            {
                Take(LexemeKind.Number, out var numLexeme);
                Take(LexemeKind.RightBracket);

                return new Output(int.Parse(numLexeme.Content));
            }

            return new Output(null);
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
