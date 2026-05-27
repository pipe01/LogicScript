using System;
using System.Threading;

namespace LogicScript.Parsing
{
    public readonly struct NodeID
    {
        private static int NextID = 999;

        private readonly int ID;

        private NodeID(int id)
        {
            this.ID = id;
        }

        public static NodeID Next()
        {
            return new NodeID(Interlocked.Increment(ref NextID));
        }

        public override int GetHashCode() => HashCode.Combine(ID);

        public override bool Equals(object? obj) => obj is NodeID other && this.ID == other.ID;

        public static bool operator ==(NodeID a, NodeID b) => a.ID == b.ID;
        public static bool operator !=(NodeID a, NodeID b) => a.ID != b.ID;
    }
}
