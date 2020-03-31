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
            var l = new Lexer(@"when (in[2], in[1]) = asd
	out[3] = 1");

            var ls = l.Lex().ToArray();
            var a = new Parser(ls).TakeCase();
        }
    }
}
