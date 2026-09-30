using System;
using System.Collections.Generic;
using LogicScript.Data;
using LogicScript.Parsing.Structures.Blocks;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class FunctionCallExpression(SourceSpan span, SourceSpan nameSpan, FunctionBlock function, Expression[] arguments) : Expression(span), IHasNameSpan
    {
        public FunctionBlock Function { get; } = function;
        public Expression[] Arguments { get; } = arguments;

        public SourceSpan NameSpan { get; } = nameSpan;

        public override bool IsConstant => false;
        public override int BitSize => Function.ResultSize;

        public override Integer ResultType => Function.ResultType;

        public override string ToString() => $"{Function.Name}({string.Join<Expression>(", ", Arguments)})";

        public override IEnumerable<ICodeNode> GetChildren() => Arguments;

        public override BitsValue GetValue(in GetValueContext ctx) => throw new NotImplementedException("Cannot call functions on interpreter mode");
    }
}
