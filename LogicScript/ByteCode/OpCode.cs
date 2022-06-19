using System.Collections.Generic;

namespace LogicScript.ByteCode
{
    public readonly struct OpCode
    {
        public string ShortName { get; }
        public IReadOnlyList<(string Name, int Bytes)> Arguments { get; }
        public int StackDelta { get; }

        internal OpCode(string shortName, IReadOnlyList<(string Name, int Bytes)> args, int stackDelta)
        {
            this.ShortName = shortName;
            this.Arguments = args;
            this.StackDelta = stackDelta;
        }
    }
}