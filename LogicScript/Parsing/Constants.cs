using LogicScript.Parsing.Structures;
using System.Collections.Generic;
using System.Linq;

namespace LogicScript.Parsing
{
    internal static class Constants
    {
        public static readonly IReadOnlyDictionary<string, Operator> AggregationOperators = new Dictionary<string, Operator>
        {
            ["and"] = Operator.And,
            ["or"] = Operator.Or,
            ["trunc"] = Operator.Truncate,
        };

        public static readonly IReadOnlyDictionary<string, Operator> OperatorShortcuts = new Dictionary<string, Operator>
        {
            ["!"] = Operator.Not,

            ["+"] = Operator.Add,
            ["-"] = Operator.Subtract,

            ["=="] = Operator.Equals,
            [">"] = Operator.Greater,
            [">="] = Operator.GreaterOrEqual,
            ["<"] = Operator.Lesser,
            ["<="] = Operator.LesserOrEqual,

            ["&"] = Operator.And,
            ["|"] = Operator.Or,
        };
        
        public static readonly IReadOnlyDictionary<Operator, int> OperatorPrecedence = new Dictionary<Operator, int>
        {
            [Operator.Equals] = 0,
            [Operator.Greater] = 0,
            [Operator.GreaterOrEqual] = 0,
            [Operator.Lesser] = 0,
            [Operator.LesserOrEqual] = 0,

            [Operator.Add] = 1,
            [Operator.Subtract] = 1,

            [Operator.Multiply] = 2,
            [Operator.Divide] = 2,

            [Operator.And] = 3,
            [Operator.Or] = 3,
        };

        public static readonly string[] Keywords = new[]
        {
            "when", "once", "any", "end", "in", "out",
        }.Concat(AggregationOperators.Keys).ToArray();
    }
}
