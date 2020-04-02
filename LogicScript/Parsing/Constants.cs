using LogicScript.Parsing.Structures;
using System.Collections.Generic;
using System.Linq;

namespace LogicScript.Parsing
{
    internal static class Constants
    {
        public static readonly IReadOnlyDictionary<string, Operator> Operators = new Dictionary<string, Operator>
        {
            ["and"] = Operator.And,
            ["or"] = Operator.Or,
        };

        public static readonly string[] Keywords = new[]
        {
            "when", "end", "in", "out",
        }.Concat(Operators.Keys).ToArray();
    }
}
