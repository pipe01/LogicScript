using LogicScript.Parsing.Structures;
using System.Collections.Generic;
using System.Linq;

namespace LogicScript.Parsing
{
    internal static class Constants
    {
        public static readonly IReadOnlyDictionary<string, Operator> ExplicitOperators = new Dictionary<string, Operator>
        {
            ["and"] = Operator.And,
            ["or"] = Operator.Or,
            ["sum"] = Operator.Add,
            ["trunc"] = Operator.Truncate,
        };

        public static readonly string[] Keywords = new[]
        {
            "when", "once", "any", "end", "in", "out", "mem", "if", "else", "update"
        }.Concat(ExplicitOperators.Keys).ToArray();
    }
}
