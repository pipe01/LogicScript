using System;

namespace LogicScript.Parsing.Structures
{
    public abstract class InputSpec { }

    public sealed class WholeInputSpec : InputSpec { }

    public sealed class CompoundInputSpec : InputSpec
    {
        public int[] Indices { get; }

        public CompoundInputSpec(int[] indices)
        {
            this.Indices = indices ?? throw new ArgumentNullException(nameof(indices));
        }
    }
}
