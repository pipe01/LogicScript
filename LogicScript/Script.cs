using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using LogicScript.Interpreting;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicScript
{
    public class Script
    {
        public IDictionary<string, PortInfo> Inputs { get; } = new Dictionary<string, PortInfo>();
        public IDictionary<string, PortInfo> Outputs { get; } = new Dictionary<string, PortInfo>();
        public IDictionary<string, PortInfo> Registers { get; } = new Dictionary<string, PortInfo>();

        internal int RegisteredInputLength => Inputs.Values.Sum(o => o.BitSize);
        internal int RegisteredOutputLength => Outputs.Values.Sum(o => o.BitSize);

        internal IList<Block> Blocks { get; } = [];

        public string FileName { get; }
        public IReadOnlyList<Error> Errors { get; }

        public bool HasErrors => Errors.Count > 0;

        internal Script(string fileName, IReadOnlyList<Error> errors)
        {
            this.FileName = fileName;
            this.Errors = errors;
        }

        // For tests
        internal Script() : this("", [])
        {
        }

        internal ICodeNode? GetNodeAt(SourceLocation loc, Type[]? types = null)
        {
            foreach (var item in Blocks)
            {
                var node = Inner(loc, item, types);

                if (node != null)
                    return node;
            }

            return null;

            static ICodeNode? Inner(SourceLocation loc, ICodeNode node, Type[]? types)
            {
                foreach (var child in node.GetChildren())
                {
                    var childNode = Inner(loc, child, types);

                    if (childNode != null)
                        return childNode;
                }

                var nodeType = node.GetType();
                if ((types == null || types.Any(t => t.IsAssignableFrom(nodeType))) && node.Span.Contains(loc))
                    return node;

                return null;
            }
        }

        public static (Script? Script, IReadOnlyList<Error> Errors) Parse(string source, string fileName = "<script>")
        {
            var errors = new ErrorSink();

            var input = new AntlrInputStream(source.Replace("\r\n", "\n") + "\n")
            {
                name = fileName
            };
            var lexer = new LogicScriptLexer(input);
            var stream = new CommonTokenStream(lexer);
            var parser = new LogicScriptParser(stream);
            parser.AddErrorListener(new ErrorListener(errors));

            Script? script = null;

            try
            {
                var scriptCtx = parser.script();

                if (scriptCtx == null)
                {
                    errors.AddError("Expected script file", new SourceSpan(), true);
                }
                else
                {
                    script = new ScriptVisitor(errors).Visit(scriptCtx);
                }
            }
            catch (ParseException ex)
            {
                errors.AddError(ex.Message, ex.Span);
            }
            catch (NotConstantException ex)
            {
                errors.AddError(ex.Message, ex.Node);
            }
            catch (ParseCanceledException)
            {
            }

            return (script, errors);
        }

        internal static (Expression? Parsed, IReadOnlyCollection<Error> Errors) ParseExpression(string expression, Script script, IReadOnlyCollection<LocalInfo> locals)
        {
            var errors = new ErrorSink();
            var scriptContext = new ScriptContext(script, errors);
            var blockContext = new BlockContext(scriptContext);

            foreach (var local in locals)
            {
                blockContext.Locals.Add(local);
            }

            var input = new AntlrInputStream(expression.Replace("\r\n", "\n") + "\n")
            {
                name = "<expression>"
            };
            var lexer = new LogicScriptLexer(input);
            var stream = new CommonTokenStream(lexer);
            var parser = new LogicScriptParser(stream);
            parser.AddErrorListener(new ErrorListener(errors));

            Expression? parsed = null;

            try
            {
                parsed = new ExpressionVisitor(blockContext).Visit(parser.expression());
            }
            catch (ParseException ex)
            {
                errors.AddError(ex.Message, ex.Span);
            }
            catch (NotConstantException ex)
            {
                errors.AddError(ex.Message, ex.Node);
            }
            catch (ParseCanceledException)
            {
            }

            return (parsed, errors);
        }
    }
}
