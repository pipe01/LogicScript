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
@"
#when (in[2], in[1]) = 1010
#	out[3] = 1
#	out = '123 #nice
#	out = (1, 0, in[2])
#end

#asdasd asd asd

when in = 10101
    out = ((1,0), 1)
end

when (in[0], in[1]) = 10
    out = '123 asd
end
");

            var errors = new ErrorSink();

            var ls = l.Lex().ToArray();
            var a = new Parser(ls, errors).Parse();

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
        private int Counter = 0;

        public int InputCount => 5;

        public bool GetInput(int i)
        {
            var v = Counter++ % 2 == 0;
            Console.WriteLine($"Read input {i}: {v}");
            return v;
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
