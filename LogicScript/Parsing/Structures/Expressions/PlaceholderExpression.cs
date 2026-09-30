using System;
using LogicScript.Data;
using LogicScript.Interpreting;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class PlaceholderExpression(SourceSpan span, int bitSize = 0) : Expression(span)
    {
        public override bool IsConstant => false;
        public override int BitSize => bitSize;

        public override BitsValue GetValue(in GetValueContext ctx) => throw new NotImplementedException("Cannot get value of placeholder");
    }
}
