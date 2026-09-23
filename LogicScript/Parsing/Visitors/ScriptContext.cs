using LogicScript.Data;
using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Parsing.Visitors
{
    internal sealed class ScriptContext(Script script, ErrorSink errors)
    {
        public Script Script { get; } = script;
        public ErrorSink Errors { get; } = errors;

        private int NextNodeID = 1;

        public bool DoesIdentifierExist(string iden)
            => Script.Inputs.ContainsKey(iden)
            || Script.Outputs.ContainsKey(iden)
            || Script.Registers.ContainsKey(iden);

        public int ParseBitSize(LogicScriptParser.ExpressionContext expressionContext) => ParseBitSize(expressionContext, out _);
        public int ParseBitSize(LogicScriptParser.ExpressionContext expressionContext, out Expression expression)
        {
            var value = (int)expressionContext.GetConstantValue(this, out expression);
            if (value <= 0)
            {
                Errors.AddBitLengthTooSmall(value, expressionContext.Span());
                return 1;
            }
            if (value > BitsValue.BitSize)
            {
                Errors.AddBitLengthTooLarge(value, expressionContext.Span());
                return 64;
            }

            return value;
        }

        public NodeID NewNodeID() => new(NextNodeID++);
    }
}
