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
        private Lexeme Current;
        private Lexeme NextNonWhitespace
        {
            get
            {
                for (int i = Index + 1; i < Lexemes.Length; i++)
                {
                    if (Lexemes[i].Kind != LexemeKind.Whitespace)
                        return Lexemes[i];
                }

                return default;
            }
        }

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

                    var (@case, taken) = TakeCase();

                    if (taken)
                        script.Cases.Add(@case);
                }
            }
            catch (LogicParserException)
            {
            }

            if (Errors.ContainsErrors)
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
            Current = Lexemes[Index];

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
        private bool Take(LexemeKind kind, bool error = true, bool fatal = false, string expected = null)
        {
            if (Current.Kind != kind)
            {
                if (error)
                    Error($"expected {kind}{(expected != null ? $" ({expected})" : null)}, {Current.Kind} found", fatal);

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

        public (Case Case, bool Taken) TakeCase()
        {
            Lexeme startLexeme;
   
            if (!TakeKeyword("when", out startLexeme, false)
                && !TakeKeyword("once", out startLexeme, false)
                && !TakeKeyword("any", out startLexeme, false))
                return (null, false);

            SkipWhitespaces();

            Expression condition = null;
            
            if (startLexeme.Content == "when")
            {
                condition = TakeExpression();

                SkipWhitespaces();

                if (Current.Kind == LexemeKind.Equals)
                    Error("unexpected assignment, did you mean to compare with \"==\"?", true);
            }

            Take(LexemeKind.NewLine);

            var stmtList = new List<Statement>();

            bool foundEnd = false;
            while (!IsEOF)
            {
                SkipWhitespaces(true);
                stmtList.Add(TakeStatement());
                SkipWhitespaces(true);

                if (TakeKeyword("end", error: false))
                {
                    foundEnd = true;
                    break;
                }
            }

            var stmts = stmtList.ToArray();

            if (!foundEnd)
                Error($"expected 'end' keyword to close case starting at {startLexeme.Location}");

            Case @case;

            if (startLexeme.Content == "when")
                @case = new ConditionalCase(condition, stmts, startLexeme.Location);
            else if (startLexeme.Content == "once")
                @case = new OnceCase(stmts, startLexeme.Location);
            else if (startLexeme.Content == "any")
                @case = new UnconditionalCase(stmts, startLexeme.Location);
            else
                throw new Exception("Invalid case?");

            return (@case, true);
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
            if (output.IsIndexed && !value.IsSingleBit)
                Error("expected a single bit or an expression that returns a single bit");

            SkipWhitespaces();
            Take(LexemeKind.NewLine);

            return output.IsIndexed
                ? new SetSingleOutputStatement(output.Index, value, location)
                : new SetOutputStatement(value, location);
        }

        private Expression TakeExpression()
        {
            return Inner(TakePrimary(), 0);

            bool Operator(Lexeme lexeme, out Operator op, out int predecence)
            {
                if (lexeme.Content != null && Constants.OperatorShortcuts.TryGetValue(lexeme.Content, out op))
                {
                    predecence = Constants.OperatorPrecedence[op];
                    return true;
                }

                op = default;
                predecence = 0;
                return false;
            }

            Expression Inner(Expression lhs, int minPredecence)
            {
                SkipWhitespaces();

                var start = Current.Location;
                while (Operator(Current, out var op, out var predecence) && predecence >= minPredecence)
                {
                    Advance();
                    SkipWhitespaces();

                    var rhs = TakePrimary();
                    SkipWhitespaces();

                    while (Operator(Current, out var lookaheadOp, out int lookaheadPrecedence) && lookaheadPrecedence > predecence)
                    {
                        rhs = Inner(rhs, lookaheadPrecedence);
                    }

                    lhs = new OperatorExpression(op, lhs, rhs, start);
                }
                return lhs;
            }

            Expression TakePrimary()
            {
                if (Peek(LexemeKind.Operator, "!"))
                {
                    Take(LexemeKind.Operator, out var opLex);
                    return new UnaryOperatorExpression(Structures.Operator.Not, TakePrimary(), opLex.Location);
                }
                else if (Take(LexemeKind.LeftParenthesis, out var par, error: false))
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
                else if (TakeKeyword("in", out var inLex, false))
                {
                    return Peek(LexemeKind.LeftBracket)
                        ? (Expression)new SingleInputExpression(TakeInput(false), inLex.Location)
                        : new WholeInputExpression(inLex.Location);
                }
                else if (Take(LexemeKind.Number, out var n, false))
                {
                    int @base = 2;

                    if (Take(LexemeKind.Apostrophe, false))
                    {
                        @base = 10;
                    }
                    else if (n.Content?.ContainsDecimalDigits() ?? false)
                    {
                        Errors.AddWarning(Current.Location, "decimal number must be suffixed");
                        @base = 10;
                    }

                    return new NumberLiteralExpression(n.Location, Convert.ToUInt64(n.Content, @base), n.Content?.Length ?? 0);
                }
                else if (Peek(LexemeKind.Keyword) && Constants.AggregationOperators.TryGetValue(Current.Content, out var op))
                {
                    return TakeExplicitOperator(op);
                }
                else
                {
                    Error("expected expression", true);
                    throw new Exception(); //Not reached
                }
            }
        }

        private Expression TakeExplicitOperator(Operator op)
        {
            Take(LexemeKind.Keyword, out var keyword);
            Take(LexemeKind.LeftParenthesis, expected: "argument open", fatal: true);

            var expr = TakeExpression();

            Take(LexemeKind.RightParenthesis, expected: "argument close", fatal: true);

            return new UnaryOperatorExpression(op, expr, keyword.Location);
        }

        private (bool IsIndexed, int Index) TakeOutput()
        {
            TakeKeyword("out");

            if (Take(LexemeKind.LeftBracket, false))
            {
                Take(LexemeKind.Number, out var numLexeme);
                Take(LexemeKind.RightBracket, expected: "output index close", fatal: true);

                return (true, int.Parse(numLexeme.Content));
            }

            return (false, 0);
        }

        private int TakeInput(bool takeKeyword = true)
        {
            if (takeKeyword)
                TakeKeyword("in");
            Take(LexemeKind.LeftBracket, expected: "input index open", fatal: true);
            Take(LexemeKind.Number, out var numLexeme, expected: "input index number");
            Take(LexemeKind.RightBracket, expected: "input index close", fatal: true);

            return int.Parse(numLexeme.Content);
        }
    }
}
