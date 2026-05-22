using System.Collections.Generic;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal class TernaryOperatorExpression(SourceSpan span, Expression condition, Expression ifTrue, Expression ifFalse) : Expression(span)
    {
        public Expression Condition { get; set; } = condition;
        public Expression IfTrue { get; set; } = ifTrue;
        public Expression IfFalse { get; set; } = ifFalse;

        // This doesn't really make sense logically since if the condition is constant there's no point
        // in having a branch, however it could be useful in the case where the condition depends on constants
        // used as parameters.
        public override bool IsConstant => Condition.IsConstant && IfTrue.IsConstant && IfFalse.IsConstant;

        // Ideally this would be calculated at runtime after knowing which branch we are going down on,
        // but the best we can do at parse time is to get the length of whichever is longest.
        public override int BitSize => IfTrue.BitSize > IfFalse.BitSize ? IfTrue.BitSize : IfFalse.BitSize;

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Condition;
            yield return IfTrue;
            yield return IfFalse;
        }

        public override string ToString() => $"If({Condition}, {IfTrue}, {IfFalse})";
    }
}
