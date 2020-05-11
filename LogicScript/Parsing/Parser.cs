using LogicScript.Data;
using LogicScript.Parsing.Structures;
using LogicScript.Utils;
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
        private Script Script;

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

            if (Lexemes.Length == 0)
                return script;

            this.Script = script;

            Current = Lexemes[0];

            try
            {
                while (!IsEOF)
                {
                    SkipWhitespaces(true);

                    script.TopLevelNodes.Add(TakeTopLevel());
                }
            }
            catch (LogicParserException)
            {
            }

            this.Script = null;

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
        private T Error<T>(string msg, bool fatal = false, SourceLocation? on = null)
        {
            Error(msg, fatal, on);
            return default;
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

        /// <summary>
        /// Takes at least one newline and any amount of whitespace.
        /// </summary>
        [DebuggerStepThrough]
        private void TakeNewlines()
        {
            SkipWhitespaces();
            Take(LexemeKind.NewLine);
            SkipWhitespaces(true);
        }

        private TopLevelNode TakeTopLevel()
        {
            if (Take(LexemeKind.AtSign, out var atLexeme, false))
            {
                return TakeDirective(atLexeme.Location);
            }
            else
            {
                var (@case, taken) = TakeCase();

                if (taken)
                    return @case;
                else
                    Error($"expected case, found {Current.Kind}", true);
            }

            throw null; //Not reached
        }

        private Directive TakeDirective(SourceLocation startLocation)
        {
            Take(LexemeKind.String, out var nameLexeme);
            Take(LexemeKind.Whitespace);
            Take(LexemeKind.String, out var valueLexeme);

            var name = nameLexeme.Content;
            var value = valueLexeme.Content;

            bool? onOff = value == "on" ? true :
                          value == "off" ? false : (bool?)null;

            switch (name)
            {
                case "strict":
                    Script.Strict = onOff ?? Error<bool>($"Invalid 'strict' directive value '{value}'", on: valueLexeme.Location);
                    break;
                case "suffix":
                    Script.AutoSuffix = onOff ?? Error<bool>($"Invalid 'suffix' directive value '{value}'", on: valueLexeme.Location);
                    break;
                case "precompute":
                    Script.Precompute = onOff ?? Error<bool>($"Invalid 'precompute' directive value '{value}'", on: valueLexeme.Location);
                    break;

                default:
                    Error($"Invalid directive '{name}'", on: nameLexeme.Location);
                    break;
            }

            return new Directive(name, value, startLocation);
        }

        private (Case Case, bool Taken) TakeCase()
        {
            Lexeme startLexeme;

            if (!TakeKeyword("when", out startLexeme, false)
                && !TakeKeyword("once", out startLexeme, false)
                && !TakeKeyword("any", out startLexeme, false))
            {
                return (null, false);
            }

            SkipWhitespaces();

            Expression condition = null;

            if (startLexeme.Content == "when")
            {
                condition = TakeExpression();

                SkipWhitespaces();

                if (Current.Kind == LexemeKind.EqualsAssign)
                    Error("unexpected assignment, did you mean to compare with \"==\"?", true);
            }

            Take(LexemeKind.NewLine);

            var stmts = TakeStatements(startLexeme.Location);

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

        private IReadOnlyList<Statement> TakeStatements(SourceLocation startLocation, Func<bool> end = null)
            => TakeStatements(startLocation, out _, end);

        private IReadOnlyList<Statement> TakeStatements(SourceLocation startLocation, out Lexeme endLexeme, Func<bool> end = null)
        {
            end = end ?? (() => TakeKeyword("end", false));
            endLexeme = Current;

            var stmtList = new List<Statement>();

            SkipWhitespaces(true);

            while (!end())
            {
                if (IsEOF)
                    Error($"expected end of block starting at {startLocation}, found end of file", true);

                stmtList.Add(TakeStatement());

                Take(LexemeKind.NewLine);
                SkipWhitespaces(true);
                endLexeme = Current;
            }

            return stmtList;
        }

        private Statement TakeStatement()
        {
            SkipWhitespaces(true);

            if (!(TryTakeIfStatement(out var statement)
                || TryTakeForStatement(out statement)
                //|| TryTakeAssignStatement(out statement)
                || TryTakeQueueUpdateStatement(out statement)))
            {
                return TakeExpressionStatement();
            }

            return statement;
        }

        private Statement TakeExpressionStatement()
        {
            return new ExpressionStatement(TakeExpression(), Current.Location);
        }

        private bool TryTakeIfStatement(out Statement statement)
        {
            if (!TakeKeyword("if", out var start, false))
            {
                statement = null;
                return false;
            }

            SkipWhitespaces();

            var condition = TakeExpression();

            TakeNewlines();

            var body = TakeStatements(start.Location, out var endLexeme, () => TakeKeyword("end", false) || TakeKeyword("else", false));

            IReadOnlyList<Statement> @else = null;
            if (endLexeme.Content == "else")
                @else = TakeStatements(endLexeme.Location);

            statement = new IfStatement(condition, body, @else, start.Location);
            return true;
        }

        private bool TryTakeForStatement(out Statement statement)
        {
            if (!TakeKeyword("for", out var start, false))
            {
                statement = null;
                return false;
            }

            Take(LexemeKind.Whitespace);
            SkipWhitespaces();

            Take(LexemeKind.String, out var varName, expected: "for variable name");

            Take(LexemeKind.Whitespace);
            SkipWhitespaces();

            Expression from;

            if (TakeKeyword("from", false))
            {
                SkipWhitespaces();
                from = TakeExpression();
                SkipWhitespaces();
            }
            else
            {
                from = new NumberLiteralExpression(BitsValue.Zero, Current.Location);
            }

            TakeKeyword("to");
            SkipWhitespaces();

            var to = TakeExpression();

            var body = TakeStatements(start.Location);

            statement = new ForStatement(varName.Content, from, to, body, start.Location);
            return true;
        }

        private bool TryTakeQueueUpdateStatement(out Statement statement)
        {
            if (TakeKeyword("update", out var lexeme, false))
            {
                statement = new QueueUpdateStatement(lexeme.Location);
                return true;
            }

            statement = null;
            return false;
        }

        private Operator ConvertToOperator(LexemeKind kind)
        {
            switch (kind)
            {
                case LexemeKind.EqualsAssign: return Operator.Assign;
                case LexemeKind.Add: return Operator.Add;
                case LexemeKind.Subtract: return Operator.Subtract;
                case LexemeKind.Multiply: return Operator.Multiply;
                case LexemeKind.Divide: return Operator.Divide;
                case LexemeKind.Equals: return Operator.Equals;
                case LexemeKind.NotEquals: return Operator.NotEquals;
                case LexemeKind.Greater: return Operator.Greater;
                case LexemeKind.GreaterOrEqual: return Operator.GreaterOrEqual;
                case LexemeKind.Lesser: return Operator.Lesser;
                case LexemeKind.LesserOrEqual: return Operator.LesserOrEqual;
                case LexemeKind.And: return Operator.And;
                case LexemeKind.Or: return Operator.Or;
                case LexemeKind.Xor: return Operator.Xor;
                case LexemeKind.BitShiftLeft: return Operator.BitShiftLeft;
                case LexemeKind.BitShiftRight: return Operator.BitShiftRight;
            }

            return Operator.None;
        }

        private Expression TakeExpression()
        {
            return Inner(TakePrimaryExpression(), 0);

            bool DoOperator(Lexeme lexeme, out Operator op, out int predecence)
            {
                if (lexeme.Content != null)
                {
                    op = ConvertToOperator(lexeme.Kind);
                    if (op == Operator.None)
                        goto none;

                    predecence = (int)op;
                    return true;
                }

            none:
                op = default;
                predecence = 0;
                return false;
            }

            Expression Inner(Expression lhs, int minPredecence)
            {
                SkipWhitespaces();

                var start = Current.Location;
                while (DoOperator(Current, out var op, out var predecence) && predecence >= minPredecence)
                {
                    Advance();
                    SkipWhitespaces();

                    var rhs = TakePrimaryExpression();
                    SkipWhitespaces();

                    while (DoOperator(Current, out var lookaheadOp, out int lookaheadPrecedence) && lookaheadPrecedence > predecence)
                    {
                        rhs = Inner(rhs, lookaheadPrecedence);
                    }

                    lhs = new OperatorExpression(op, lhs, rhs, start);
                }
                return lhs;
            }
        }

        private Expression TakePrimaryExpression()
        {
            Expression expr;

            if (Take(LexemeKind.Not, out var opLex, false))
            {
                expr = new UnaryOperatorExpression(Operator.Not, TakePrimaryExpression(), opLex.Location);
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
                    expr = values[0];
                else
                    expr = new ListExpression(values.ToArray(), par.Location);
            }
            else if (TryTakeSlot(out var slot))
            {
                expr = slot;
            }
            else if (Take(LexemeKind.Number, out var n, false))
            {
                int @base = 2;
                int length = n.Content.Length;

                if (Take(LexemeKind.Apostrophe, false))
                {
                    @base = 10;
                }
                else if (n.Content.ContainsDecimalDigits())
                {
                    if (!Script.AutoSuffix)
                        Errors.AddWarning(Current.Location, "decimal number must be suffixed");

                    @base = 10;
                }

                ulong num = Convert.ToUInt64(n.Content, @base);

                if (@base == 10)
                {
                    length = BitUtils.GetBitSize(num);
                }

                expr = new NumberLiteralExpression(new BitsValue(num, length), n.Location);
            }
            else if (Peek(LexemeKind.String))
            {
                Take(LexemeKind.String, out var nameLexeme);

                if (Peek(LexemeKind.LeftParenthesis))
                    expr = TakeFunctionCall(nameLexeme);
                else
                    expr = new VariableAccessExpression(nameLexeme.Content, nameLexeme.Location);
            }
            else
            {
                Error("expected expression", true);
                throw new Exception(); //Not reached
            }

            if (Take(LexemeKind.LeftBracket, false))
            {
                var start = TakeExpression();
                Expression end = null;
                bool hasEnd = true;

                if (Take(LexemeKind.DotDot, false))
                {
                    if (Peek(LexemeKind.RightBracket))
                        hasEnd = false;
                    else
                        end = TakeExpression();
                }

                Take(LexemeKind.RightBracket);

                expr = new IndexerExpression(expr, start, end, hasEnd, expr.Location);
            }

            return expr;
        }

        private Expression TakeFunctionCall(Lexeme nameLexeme)
        {
            Take(LexemeKind.LeftParenthesis, expected: "argument list opening parenthesis");

            var args = new List<Expression>();

            do
            {
                SkipWhitespaces(true);

                args.Add(TakeExpression());

                SkipWhitespaces(true);
            } while (Take(LexemeKind.Comma, false));

            Take(LexemeKind.RightParenthesis, expected: "argument list closing parenthesis");

            return new FunctionCallExpression(nameLexeme.Content, args, nameLexeme.Location);
        }

        private bool TryTakeSlot(out SlotExpression s)
        {
            Lexeme lexeme = Current;
            Slots slot;

            if (TakeKeyword("in", false))
                slot = Slots.In;
            else if (TakeKeyword("out", false))
                slot = Slots.Out;
            else if (TakeKeyword("mem", false))
                slot = Slots.Memory;
            else
            {
                s = null;
                return false;
            }

            s = new SlotExpression(slot, lexeme.Location);
            return true;
        }
    }
}
