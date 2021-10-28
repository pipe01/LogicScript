using Antlr4.Runtime;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Visitors;
using System;
using System.Collections.Generic;

namespace LogicScript
{
    public class Script
    {
        internal IDictionary<string, PortInfo> Inputs { get; } = new Dictionary<string, PortInfo>();
        internal IDictionary<string, PortInfo> Outputs { get; } = new Dictionary<string, PortInfo>();
        internal IDictionary<string, PortInfo> Registers { get; } = new Dictionary<string, PortInfo>();

        internal IList<WhenBlock> Blocks { get; } = new List<WhenBlock>();

        public static Script Parse(string source)
        {
            var input = new AntlrInputStream(source + Environment.NewLine);
            var lexer = new LogicScriptLexer(input);
            var stream = new CommonTokenStream(lexer);
            var parser = new LogicScriptParser(stream);
            parser.AddErrorListener(new ErrorListener());

            return new ScriptVisitor().Visit(parser.script());
        }
    }
}
