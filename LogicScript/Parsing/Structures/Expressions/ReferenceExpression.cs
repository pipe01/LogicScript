using System.Collections.Generic;
using LogicScript.Data;
using LogicScript.Interpreting;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal sealed class ReferenceExpression(SourceSpan span, Reference target) : Expression(span)
    {
        public Reference Reference => target;

        public override bool IsConstant => target.IsConstant;
        public override int BitSize => Reference.BitSize;

        public override string ToString() => Reference.ToString() ?? "<unknown reference>";

        public override IEnumerable<ICodeNode> GetChildren()
        {
            yield return Reference;
        }

        public override BitsValue GetValue(in GetValueContext ctx)
        {
            switch (Reference)
            {
                case PortReference port:
                    {
                        var vectorIndex = port.VectorIndex == null ? 0 : (int)port.VectorIndex.GetValue(ctx).Number;
                        if (vectorIndex >= port.PortInfo.VectorLength)
                            throw new InterpreterException("Vector index out of range", port.VectorIndex!.Span);

                        return port.PortInfo.Target switch
                        {
                            MachinePorts.Output => throw new InterpreterException("Cannot read from output", Span),
                            MachinePorts.Input => ctx.Machine == null
                                ? throw new InterpreterException("Can't access inputs on this interpreter runner")
                                : ctx.Machine.ReadInputs(port.StartIndex + port.BitSize * vectorIndex, port.BitSize),
                            MachinePorts.Register => ctx.Registers == null
                                ? throw new InterpreterException("Can't access registers on this interpreter runner")
                                : ctx.Registers.GetRegister(port.StartIndex, vectorIndex),
                            _ => throw new InterpreterException("Unknown reference target", Span),
                        };
                    }

                case LocalReference localReference:
                    if (ctx.Locals == null)
                        throw new InterpreterException("Can't access locals on interpreter runner");
                    else
                        return ctx.Locals[localReference.LocalInfo];

                case ConstantReference cnst:
                    return cnst.Constant.Value;
            }

            throw new InterpreterException("Unknown reference type", Span);
        }
    }
}
