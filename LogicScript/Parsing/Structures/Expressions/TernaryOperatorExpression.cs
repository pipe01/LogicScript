namespace LogicScript.Parsing.Structures.Expressions
{
    internal class TernaryOperatorExpression : Expression
    {
        public Expression Condition { get; set; }
        public Expression IfTrue { get; set; }
        public Expression IfFalse { get; set; }

        // This doesn't really make sense logically since if the condition is constant there's no point
        // in having a branch, however it could be useful in the case where the condition depends on constants
        // used as parameters.
        public override bool IsConstant => Condition.IsConstant && IfTrue.IsConstant && IfFalse.IsConstant;

        public TernaryOperatorExpression(SourceLocation location, Expression condition, Expression ifTrue, Expression ifFalse) : base(location)
        {
            this.Condition = condition;
            this.IfTrue = ifTrue;
            this.IfFalse = ifFalse;
        }

        public override string ToString() => $"If({Condition}, {IfTrue}, {IfFalse})";
    }
}
