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
@"when (in[2], in[1]) = (1, in[2], 0)
	out[3] = 1
	out = 123'
	out = (1, 0, in[2])
end");

            var ls = l.Lex().ToArray();
            var a = new Parser(ls).TakeCase();
        }
    }
}
