using System.Collections.Generic;
using LogicScript.Data;

namespace LogicScript.Parsing.Structures.Expressions
{
    internal readonly record struct GetValueContext(IMachine? Machine, IRegistersInstance? Registers, IReadOnlyDictionary<LocalInfo, ulong>? Locals)
    {
        public static readonly GetValueContext Empty = new();
    }

    internal abstract class Expression(SourceSpan span) : ICodeNode
    {
        public SourceSpan Span { get; } = span;

        public abstract bool IsConstant { get; }
        public abstract int BitSize { get; }

        public virtual Integer ResultType => BitSize.ToIntegerSize();

        public virtual IEnumerable<ICodeNode> GetChildren()
        {
            yield break;
        }

        /// <summary>
        /// Only used when computing constant values while parsing, for constant folding and for executing expressions entered while debugging,
        /// which means that no statement execution is required.
        /// </summary>
        public abstract BitsValue GetValue(in GetValueContext ctx);
        public BitsValue GetValue() => GetValue(GetValueContext.Empty);
    }
}
