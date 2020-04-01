﻿using LogicScript.Parsing.Structures;
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

        public Script? Parse()
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
                }
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
                throw new Exception();
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

            var inputsValue = TakeExpression();

            //if (inputSpec is CompoundInputSpec comp && inputsValue.Values.Length != comp.Indices.Length)
            //    Error("mismatched input count");

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
                if (!(value is NumberLiteralExpression num) || num.Length != 1)
                    Error("expected a single bit (0 or 1)");
            }

            SkipWhitespaces();
            Take(LexemeKind.NewLine);

            return new OutputSetStatement(output, value, location);
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
            if (Take(LexemeKind.LeftParenthesis, error: false))
            {
                var values = new List<Expression>();

                do
                {
                    SkipWhitespaces();
                    values.Add(TakeExpression());
                } while (Take(LexemeKind.Comma, false));

                Take(LexemeKind.RightParenthesis);

                return new ListExpression(values.ToArray());
            }
            else
            {
                int @base = 2;
                if (Take(LexemeKind.Apostrophe, false))
                    @base = 10;

                if (!Take(LexemeKind.Number, out var n))
                {
                    Error("expected expression", true);
                    return NumberLiteralExpression.Zero;
                }

                if (@base == 2 && n.Content!.ContainsDecimalDigits())
                {
                    Error("decimal number must be prefixed");
                    return NumberLiteralExpression.Zero;
                }

                return new NumberLiteralExpression(Convert.ToInt32(n.Content, @base), n.Content!.Length);
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