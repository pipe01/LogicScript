using LogicScript;
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
@"when (in[2], in[1]) = 1010
	out[3] = 1
	out = 123' #nice
	out = (1, 0, in[2])
end

#asdasd asd asd

when (in[0], in[1]) = 10
    out = 123'
end
");

            var errors = new ErrorSink();

            var ls = l.Lex().ToArray();
            var a = new Parser(ls, errors).Parse();

            var engine = new LogicEngine(a);
            engine.DoUpdate(new Machine());

            foreach (var item in errors)
            {
                Console.WriteLine(item);
            }
        }
    }

    public class Machine : IMachine
    {
        private int Counter = 0;

        public bool GetInput(int i)
        {
            var v = Counter++ % 2 == 0;
            Console.WriteLine($"Read input {i}: {v}");
            return v;
        }

        public Memory<bool> GetInputs()
        {
            Console.WriteLine("Get all inputs");
            throw new NotImplementedException();
        }

        public void SetOutput(int i, bool on)
        {
            Console.WriteLine($"Set output {i} to {on}");
        }

        public void SetOutputs(Span<bool> values)
        {
            Console.WriteLine($"Set outputs to ({string.Join(", ", values.ToArray())})");
        }
    }
}
