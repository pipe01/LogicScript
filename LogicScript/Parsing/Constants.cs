using LogicScript.Parsing.Structures;
using System.Collections.Generic;
using System.Linq;

namespace LogicScript.Parsing
{
    internal static class Constants
    {
        public static readonly IReadOnlyDictionary<string, Operator> Operators = new Dictionary<string, Operator>
        {
            ["add"] = Operator.Add,
            ["sub"] = Operator.Subtract,

            ["and"] = Operator.And,
            ["or"] = Operator.Or,
        };

        public static readonly IReadOnlyDictionary<string, Operator> OperatorShortcuts = new Dictionary<string, Operator>
        {
            ["+"] = Operator.Add,
            ["-"] = Operator.Subtract,

            ["&"] = Operator.And,
            ["|"] = Operator.Or,
        };
        
        public static readonly IReadOnlyDictionary<Operator, int> OperatorPrecedence = new Dictionary<Operator, int>
        {
            [Operator.Add] = 1,
            [Operator.Subtract] = 1,

            [Operator.And] = 2,
            [Operator.Or] = 2,
        };

        public static readonly string[] Keywords = new[]
        {
            "when", "end", "in", "out",
        }.Concat(Operators.Keys).ToArray();
    }
}
