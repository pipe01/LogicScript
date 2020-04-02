using System;

namespace LogicScript.Parsing.Structures
{
    internal abstract class InputSpec { }

    internal sealed class WholeInputSpec : InputSpec { }

    internal sealed class CompoundInputSpec : InputSpec
    {
        public int[] Indices { get; }

        public CompoundInputSpec(int[] indices)
        {
            this.Indices = indices ?? throw new ArgumentNullException(nameof(indices));
        }
    }

    internal sealed class SingleInputSpec : InputSpec
    {
        public int Index { get; }

        public SingleInputSpec(int index)
        {
            this.Index = index;
        }
    }
}
