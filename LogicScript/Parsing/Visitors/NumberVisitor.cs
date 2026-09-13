using Antlr4.Runtime.Misc;
using LogicScript.Data;
using System;

namespace LogicScript.Parsing.Visitors
{
    internal class NumberVisitor : LogicScriptParserBaseVisitor<BitsValue>
    {
        public override BitsValue VisitNumber([NotNull] LogicScriptParser.NumberContext context)
        {
            if (context.DEC_NUMBER() != null)
            {
                return ulong.Parse(context.DEC_NUMBER().GetText().Replace("_", ""));
            }
            else if (context.BIN_NUMBER() != null)
            {
                var numStr = context.BIN_NUMBER().GetText().TrimEnd('b').Replace("_", "");

                return new BitsValue(Convert.ToUInt64(numStr, 2), numStr.Length);
            }
            else if (context.HEX_NUMBER() != null)
            {
                return Convert.ToUInt64(context.HEX_NUMBER().GetText().Replace("_", ""), 16);
            }

            throw new ParseException("Unknown number type", context.Span());
        }
    }
}
