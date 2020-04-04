using System.Collections.Generic;
using System.Linq;

namespace LogicScript.Parsing.Structures
{
    public class Script
    {
        internal IList<Case> Cases { get; } = new List<Case>();

        public static CompilationResult Compile(string script)
        {
            var errors = new ErrorSink();
            var lexemes = new Lexer(script, errors).Lex().ToArray();

            if (errors.ContainsErrors)
                return new CompilationResult(false, null, errors);

            var parsed = new Parser(lexemes, errors).Parse();

            if (errors.ContainsErrors)
                return new CompilationResult(false, null, errors);

            return new CompilationResult(true, parsed, null);
        }

        public readonly struct CompilationResult
        {
            public bool Success { get; }

            public Script Script { get; }
            public IEnumerable<Error> Errors { get; }

            internal CompilationResult(bool success, Script script, IEnumerable<Error> errors)
            {
                this.Success = success;
                this.Script = script;
                this.Errors = errors;
            }
        }
    }
}
