﻿using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using System.Collections.Generic;
using System.Linq;

namespace LogicScript
{
    public class Script
    {
        internal IList<TopLevelNode> TopLevelNodes { get; } = new List<TopLevelNode>();

        public static CompilationResult Compile(string script)
        {
            var errors = new ErrorSink();

            Lexeme[] lexemes;
            using (var lexer = new Lexer(script, errors))
                lexemes = lexer.Lex().ToArray();

            if (errors.ContainsErrors)
                return new CompilationResult(false, null, errors);

            var parsed = new Parser(lexemes, errors).Parse();

            if (errors.ContainsErrors)
                return new CompilationResult(false, null, errors);

            EvaluatorVisitor.Visit(parsed);

            return new CompilationResult(true, parsed, errors);
        }

        public readonly struct CompilationResult
        {
            public bool Success { get; }

            public Script Script { get; }
            public ErrorSink Errors { get; }

            internal CompilationResult(bool success, Script script, ErrorSink errors)
            {
                this.Success = success;
                this.Script = script;
                this.Errors = errors;
            }
        }
    }
}
