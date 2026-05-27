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
    }
}