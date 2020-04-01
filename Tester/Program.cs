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
when in = 1010
    # Set individual output bits
    out[0] = 1
    out[2] = 0
    out[1] = in[2]

    # Set all the output bits
    out = 1010
    out = 14'  #The ' denotes that it's a decimal number instead of a binary one
end

# Other example case statements:
# when in = (1, 0, in[1], 1)
# when in = 12'
# when (in[0], in[1]) = 10
# when (in[2], in[1]) = 3'
");

            var errors = new ErrorSink();

            var ls = l.Lex().ToArray();
            Script script = null;

            try
            {
                script = new Parser(ls, errors).Parse();
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

            var engine = new LogicEngine(script);
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

            Console.WriteLine($"Set outputs to ({string.Join(", ", values.AsMemory().ToArray())})");
        }
    }
}
