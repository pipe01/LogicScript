using LogicScript;
using LogicScript.Data;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using System;
using System.Linq;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            var l = new Lexer(
@"
#when (in[2], in[1]) = 1010
#	out[3] = 1
#	out = '123 #nice
#	out = (1, 0, in[2])
#end

#asdasd asd asd

when (in[0], in[1]) = (in[0], in[1])
    out = 123'
end

when in = 10101
    out = ((1,0), 1)
end

");

            var errors = new ErrorSink();

            var ls = l.Lex().ToArray();
            Script a = null;

            try
            {
                a = new Parser(ls, errors).Parse();
            }
            catch (LogicParserException)
            {
            }

            foreach (var item in errors)
            {
                Console.WriteLine(item);
            }

            if (errors.Count > 0)
            {
                Console.ReadKey(true);
                return;
            }

            var engine = new LogicEngine(a);
            engine.DoUpdate(new Machine());
        }
    }

    public class Machine : IMachine
    {
        public int InputCount => Inputs.Length;
        public int OutputCount => Outputs.Length;

        private bool[] Inputs = new[] { true, false, true, false };
        private bool[] Outputs = new[] { true, false, true, false };

        public bool GetInput(int i)
        {
            var v = Inputs[i];
            Console.WriteLine($"Read input {i}: {v}");
            return v;
        }

        public void SetOutput(int i, bool on)
        {
            Outputs[i] = on;

            Console.WriteLine($"Set output {i} to {on}");
        }

        public void SetOutputs(BitsValue values)
        {
            values.AsMemory().CopyTo(Outputs);

            Console.WriteLine($"Set outputs to ({values.AsMemory()})");
        }
    }
}
