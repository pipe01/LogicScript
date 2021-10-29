using Antlr4.Runtime;
using LogicScript.Interpreting;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Blocks;
using LogicScript.Parsing.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicScript
{
    public class Script
    {
        internal IDictionary<string, PortInfo> Inputs { get; } = new Dictionary<string, PortInfo>();
        internal IDictionary<string, PortInfo> Outputs { get; } = new Dictionary<string, PortInfo>();
        internal IDictionary<string, PortInfo> Registers { get; } = new Dictionary<string, PortInfo>();

        internal int RegisteredInputLength => Inputs.Values.Sum(o => o.BitSize);
        internal int RegisteredOutputLength => Outputs.Values.Sum(o => o.BitSize);

        internal IList<Block> Blocks { get; } = new List<Block>();

        public void Run(IMachine machine, bool checkPortCount = true)
        {
            Interpreter.Run(this, machine, checkPortCount);
        }

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
