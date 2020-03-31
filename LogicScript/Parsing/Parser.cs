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

        private bool IsEOF => Current.Kind == LexemeKind.EOF;

        private readonly Lexeme[] Lexemes;
        private readonly ErrorSink Errors;

        public Parser(Lexeme[] lexemes, ErrorSink errors)
        {
            this.Lexemes = lexemes;
            this.Errors = errors;
        }

        public Script Parse()
        {
            var script = new Script();

            while (!IsEOF)
            {
                switch (Current.Kind)
                {
                    case LexemeKind.Keyword when Current.Content == "when":
                        script.Cases.Add(TakeCase());
                        break;
                    case LexemeKind.Whitespace:
                    case LexemeKind.NewLine:
                        Advance();
                        break;
                    case LexemeKind.Hash:
                        while (Current.Kind != LexemeKind.NewLine)
                            Advance();

                        break;
                }
            }

            return script;
        }

        [DebuggerStepThrough]
        private void Error(string msg)
        {
            Errors.AddError(Current.Location, msg);
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
        private bool TakeKeyword(string keyword, bool error = true) => TakeKeyword(keyword, out _, error);

        [DebuggerStepThrough]
        private bool TakeKeyword(string keyword, out Lexeme lexeme, bool error = true)
        {
            if (Current.Kind != LexemeKind.Keyword || Current.Content != keyword)
            {
                if (error)
                    Error($"Expected '{keyword}', {Current.Kind} found");

                lexeme = default;
                return false;
            }

            lexeme = Current;
            Advance();
            return true;
        }

        [DebuggerStepThrough]
        private bool Take(LexemeKind kind, bool error = true)
        {
            if (Current.Kind != kind)
            {
                if (error)
                    Error($"expected '{kind}', {Current.Kind} found");

                return false;
            }

            Advance();
            return true;
        }

        [DebuggerStepThrough]
        private bool Take(LexemeKind kind, out Lexeme lexeme, bool error = true, string? expected = null)
        {
            if (Current.Kind != kind)
            {
                if (error)
                    Error($"expected {expected ?? kind.ToString()}, found {Current.Kind}");

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
            TakeKeyword("when", out var startLexeme);
            SkipWhitespaces();

            var inputSpec = TakeInputSpec();

            SkipWhitespaces();
            Take(LexemeKind.Equals);
            SkipWhitespaces();

            var inputsValue = TakeBitsValue();

            SkipWhitespaces();
            Take(LexemeKind.NewLine);

            var stmts = new List<Statement>();

            bool foundEnd = false;
            while (!IsEOF)
            {
                stmts.Add(TakeStatement());

                if (TakeKeyword("end", error: false))
                {
                    foundEnd = true;
                    break;
                }
            }

            if (!foundEnd)
                Error($"Expected 'end' keyword to close case starting at {startLexeme.Location}");

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
            if (TakeKeyword("in", error: false))
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
            if (Take(LexemeKind.LeftParenthesis, error: false))
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
                if (!Take(LexemeKind.Number, out var n))
                {
                    Advance();
                    return new LiteralBitsValue(0);
                }

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
                    Error("expected bit value (0 or 1)");

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
