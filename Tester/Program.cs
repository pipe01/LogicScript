using LogicScript;
using System;
using System.Linq;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            var l = new Lexer(@"when (in[2], in[1]) = (1, 0)
	out[3] = 1");

            var ls = l.Lex().ToArray();
        }
    }
}
