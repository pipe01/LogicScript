using System;
using System.Collections.Generic;
using System.Linq;

namespace LogicScript.Parsing.Structures
{
    internal static class Extensions
    {
        public static IEnumerable<ICodeNode> GetDescendants(this ICodeNode parent, bool depthFirst = true)
        {
            if (parent == null)
                return [];

            var children = parent.GetChildren().SelectMany(o => o.GetDescendants(depthFirst));

            return depthFirst ? children.Append(parent) : children.Prepend(parent);
        }

        public static Integer ToIntegerSize(this int bitSize) => bitSize switch
        {
            <= 32 => Integer.Int,
            <= 64 => Integer.Long,
            _ => throw new ArgumentOutOfRangeException(nameof(bitSize)),
        };
        public static Type ToIntegerType(this Integer size) => size switch
        {
            Integer.Int => typeof(uint),
            Integer.Long => typeof(ulong),
            _ => throw new ArgumentException(nameof(size)),
        };
    }
}