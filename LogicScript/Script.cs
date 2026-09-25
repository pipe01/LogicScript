using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using LogicScript.Compiling;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Structures.Expressions;
using LogicScript.Parsing.Visitors;
using LogicScript.Testing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LogicScript
{
    internal record FunctionDeclaration(string Name, int ReturnSize, int[] Parameters);

    public class Script
    {
        public IDictionary<string, MachinePortInfo> Inputs { get; } = new Dictionary<string, MachinePortInfo>();
        public IDictionary<string, MachinePortInfo> Outputs { get; } = new Dictionary<string, MachinePortInfo>();
        public IDictionary<string, MachinePortInfo> Registers { get; } = new Dictionary<string, MachinePortInfo>();
        public IDictionary<string, Constant> Constants { get; } = new Dictionary<string, Constant>();
        internal IDictionary<string, FunctionBlock> Functions { get; } = new Dictionary<string, FunctionBlock>();
        public IList<TestCase> TestCases { get; } = [];

        internal int RegisteredInputLength => Inputs.Values.Sum(o => o.BitSize * o.VectorLength);
        internal int RegisteredOutputLength => Outputs.Values.Sum(o => o.BitSize * o.VectorLength);

        internal IList<Block> Blocks { get; } = [];

        public string Source { get; }
        public string FileName { get; }
        public IReadOnlyList<Error> Errors { get; }

        public bool HasErrors => Errors.Count > 0;

        internal Script(string source, string fileName, IReadOnlyList<Error> errors)
        {
            this.Source = source;
            this.FileName = fileName;
            this.Errors = errors;
        }

        // For tests
        internal Script() : this("", "", [])
        {
        }

        public IEnumerable<ICodeNode> VisitAll(bool depthFirst = true)
        {
            return Blocks.Cast<ICodeNode>()
                .Concat(TestCases.Cast<ICodeNode>())
                .Concat(Constants.Values.Cast<ICodeNode>())
                .Concat(Functions.Values.Cast<ICodeNode>())
                .SelectMany(o => o.GetDescendants(depthFirst));
        }

        public bool TryGetPort(string name, MachinePorts ports, [MaybeNullWhen(false)] out MachinePortInfo portInfo)
        {
            switch (ports)
            {
                case MachinePorts.Input:
                    return Inputs.TryGetValue(name, out portInfo);
                case MachinePorts.Output:
                    return Outputs.TryGetValue(name, out portInfo);
                case MachinePorts.Register:
                    return Registers.TryGetValue(name, out portInfo);
                default:
                    portInfo = default;
                    return false;
            }
        }

        internal ICodeNode? GetNodeAt(SourceLocation loc, Type[]? types = null, bool depthFirst = true)
        {
            foreach (var node in VisitAll(depthFirst))
            {
                var nodeType = node.GetType();

                if ((types == null || types.Any(t => t.IsAssignableFrom(nodeType))) && node.Span.Contains(loc))
                    return node;
            }

            return null;
        }

        public static (Script? Script, IReadOnlyList<Error> Errors) Parse(string source, string fileName = "<script>", bool addNewline = true)
        {
            var errors = new ErrorSink();

            var normSource = source.Replace("\r\n", "\n");
            if (addNewline)
                normSource += "\n";

            var input = new AntlrInputStream(normSource)
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
                    throw new ParseException("Expected script file", new());
                }
                else
                {
                    script = new ScriptVisitor(errors, source).Visit(scriptCtx);
                }
            }
            catch (ParseException ex)
            {
                errors.AddError(ex.Message, ex.Span);
            }
            catch (ParseCanceledException)
            {
            }
            catch (Exception e)
            {
                if (errors.Count == 0)
                    errors.AddError($"Exception while parsing: {e.GetType().FullName}", new SourceSpan());

                Debug.WriteLine($"Unhandled exception while parsing script: {e}");
            }

            return (script, errors);
        }

        internal (Expression? Parsed, IReadOnlyCollection<Error> Errors) ParseExpression(string expression, IReadOnlyCollection<LocalInfo> locals)
        {
            var errors = new ErrorSink();
            var scriptContext = new ScriptContext(this, errors);
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
            catch (ParseCanceledException)
            {
            }

            return (parsed, errors);
        }
    }

    public interface IScriptInstance
    {
        IRegisters Registers { get; }
        bool HasRun { get; set; }
        IMachine Machine { get; set; }
        IDebugger? Debugger { get; set; }

        void Run();
    }

    public interface ICompiledScript
    {
        IScriptInstance Instantiate(IMachine machine);
    }
}
