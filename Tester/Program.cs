using LogicScript;
using LogicScript.Parsing;
using System;
using System.Linq;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            var l = new Lexer(
@"when (in[2], in[1]) = 132'
	out[3] = 1
	out = 123'
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

            foreach (var item in errors)
            {
                Console.WriteLine(item);
            }
        }
    }
}
