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

            try
            {
                while (!IsEOF)
                {
                    SkipWhitespaces(true);

                    script.Cases.Add(TakeCase());
                }
            }
            catch (LogicParserException)
            {
            }

            if (Errors.Count > 0)
                return null;

            return script;
        }

        [DebuggerStepThrough]
        private void Error(string msg, bool fatal = false, SourceLocation? on = null)
        {
            Errors.AddError(on ?? Current.Location, msg);

            if (fatal)
                throw new LogicParserException();
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
                    Error($"expected {kind}, {Current.Kind} found");

                return false;
            }

            Advance();
            return true;
        }

        [DebuggerStepThrough]
        private bool Take(LexemeKind kind, out Lexeme lexeme, bool error = true, string expected = null)
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

        [DebuggerStepThrough]
        private bool Peek(LexemeKind kind, string content = null) => Current.Kind == kind && (content == null || Current.Content == content);

        public Case TakeCase()
        {
            TakeKeyword("when", out var startLexeme);
            SkipWhitespaces();

            var inputSpec = TakeInputSpec();

            SkipWhitespaces();
            Take(LexemeKind.Equals);
            SkipWhitespaces();

            var inputsValue = TakeExpression();

            //if (inputSpec is CompoundInputSpec comp && inputsValue.Values.Length != comp.Indices.Length)
            //    Error("mismatched input count");

            SkipWhitespaces();
            Take(LexemeKind.NewLine);

            var stmts = new List<Statement>();

            bool foundEnd = false;
            while (!IsEOF)
            {
                SkipWhitespaces(true);
                stmts.Add(TakeStatement());

                if (TakeKeyword("end", error: false))
                {
                    foundEnd = true;
                    break;
                }
            }

            if (!foundEnd)
                Error($"expected 'end' keyword to close case starting at {startLexeme.Location}");

            return new Case(inputSpec, inputsValue, stmts.ToArray(), startLexeme.Location);
        }

        private Statement TakeStatement()
        {
            SkipWhitespaces();

            var location = Current.Location;
            var output = TakeOutput();

            SkipWhitespaces();
            Take(LexemeKind.Equals);
            SkipWhitespaces();

            var value = TakeExpression();
            if (output.IsIndexed)
            {
                if (!value.IsSingleBit)
                    Error("expected a single bit (0 or 1)");
            }

            SkipWhitespaces();
            Take(LexemeKind.NewLine);

            return output.IsIndexed
                ? new SetSingleOutputStatement(output.Index.Value, value, location)
                : new SetOutputStatement(value, location);
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

        private Expression TakeExpression()
        {
            if (Take(LexemeKind.LeftParenthesis, out var par, error: false))
            {
                var values = new List<Expression>();

                do
                {
                    SkipWhitespaces();
                    values.Add(TakeExpression());
                } while (Take(LexemeKind.Comma, false));

                Take(LexemeKind.RightParenthesis);

                if (values.Count == 1)
                    return values[0];

                return new ListExpression(values.ToArray(), par.Location);
            }
            else if (Peek(LexemeKind.Keyword, "in"))
            {
                var loc = Current.Location;
                return new InputExpression(TakeInput(), loc);
            }
            else if (Take(LexemeKind.Number, out var n))
            {
                int @base = 2;

                if (Take(LexemeKind.Apostrophe, false))
                {
                    @base = 10;
                }
                else if (n.Content?.ContainsDecimalDigits() ?? false)
                {
                    Error("decimal number must be sufffixed");
                    @base = 10;
                }

                return new NumberLiteralExpression(n.Location, Convert.ToInt32(n.Content, @base), n.Content?.Length ?? 0);
            }
            else
            {
                Error("expected expression", true);
                throw new Exception(); //Not reached
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
