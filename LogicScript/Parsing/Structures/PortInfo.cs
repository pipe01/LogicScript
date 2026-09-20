using System;
using System.Collections.Generic;
using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Parsing.Structures
{
    public interface IPortInfo : ICodeNode, IEquatable<IPortInfo>
    {
        int BitSize { get; }
    }

    public enum MachinePorts
    {
        Placeholder,
        Input,
        Output,
        Register,
    }

    public readonly struct MachinePortInfo : IPortInfo
    {
        public string Name { get; }
        public MachinePorts Target { get; }
        /// <summary>
        /// On Inputs and Outputs, this value represents the bit at which the Input/Output vector starts.
        /// On Registers, this value represents the index of the register without taking into account vectors, i.e.
        /// the first register is at 0, the second at 1, the third at 2, etc no matter what their vector length is.
        /// </summary>
        public int StartIndex { get; }
        public int BitSize { get; }

        internal Expression? VectorLengthExpression { get; }
        /// <summary>
        /// Same as <see cref="VectorLength"/>'s constant value.
        /// </summary>
        public int VectorLength { get; }

        public SourceSpan Span { get; }

        internal MachinePortInfo(string name, MachinePorts target, int index, int bitSize, int vectorLength, Expression? lengthExpression, SourceSpan span)
        {
            if (vectorLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(vectorLength), "Vector length must be one or greater.");

            this.Name = name;
            this.Target = target;
            this.StartIndex = index;
            this.BitSize = bitSize;
            this.VectorLength = vectorLength;
            this.VectorLengthExpression = lengthExpression;
            this.Span = span;
        }

        IEnumerable<ICodeNode> ICodeNode.GetChildren()
        {
            if (VectorLengthExpression != null)
                yield return VectorLengthExpression;
        }

        public override bool Equals(object? obj) => obj is IPortInfo other && Equals(other);

        public bool Equals(IPortInfo? other)
        {
            return other is MachinePortInfo port
                && port.Target == Target
                && port.StartIndex == StartIndex
                && port.BitSize == BitSize
                && port.Span.Equals(Span);
        }

        public override int GetHashCode() => HashCode.Combine(Target, StartIndex, BitSize, Span);
    }
}
