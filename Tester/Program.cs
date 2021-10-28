using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using LogicScript;
using LogicScript.Parsing;
using LogicScript.Parsing.Visitors;
using System;

namespace Tester
{
    static class Program
    {
        static void Main(string[] args)
        {
            var script = Script.Parse(@"input asd
input test
output'2 out
asd
reg'123 on

when *
    on = out
end

");
        }
    }
}
