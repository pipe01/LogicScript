using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using LogicScript.Interpreting;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LogicScript
{
    public class Script
    {
        internal IDictionary<string, PortInfo> Inputs { get; } = new Dictionary<string, PortInfo>();
        internal IDictionary<string, PortInfo> Outputs { get; } = new Dictionary<string, PortInfo>();
        internal IDictionary<string, PortInfo> Registers { get; } = new Dictionary<string, PortInfo>();

        internal int RegisteredInputLength => Inputs.Values.Sum(o => o.BitSize);
        internal int RegisteredOutputLength => Outputs.Values.Sum(o => o.BitSize);
        internal int RegisteredRegisterLength => Registers.Values.Sum(o => o.BitSize);

        internal IList<Block> Blocks { get; } = new List<Block>();

        public void Run(IMachine machine, bool runStartup, bool checkPortCount = true)
        {
            Interpreter.Run(this, machine, runStartup, checkPortCount);
        }

        internal ICodeNode? GetNodeAt(SourceLocation loc)
        {
            foreach (var item in Blocks)
            {
                var node = Inner(loc, item);

                if (node != null)
                    return node;
            }

            return null;

            static ICodeNode? Inner(SourceLocation loc, ICodeNode node)
            {
                foreach (var child in node.GetChildren())
                {
                    var childNode = Inner(loc, child);

                    if (childNode != null)
                        return childNode;
                }

                if (node.Span.Contains(loc))
                    return node;

                return null;
            }
        }

        public static (Script? Script, IReadOnlyList<Error> Errors) Parse(string source)
        {
            var errors = new ErrorSink();

            var input = new AntlrInputStream(source.Replace("\r\n", "\n") + "\n");
            var lexer = new LogicScriptLexer(input);
            var stream = new CommonTokenStream(lexer);
            var parser = new LogicScriptParser(stream);
            parser.AddErrorListener(new ErrorListener(errors));

            Script? script = null;

            try
            {
                script = new ScriptVisitor(errors).Visit(parser.script());
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

            return (errors.Count > 0 ? null : script, errors);
        }
    }
}
