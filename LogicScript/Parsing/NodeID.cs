using System;

namespace LogicScript.Parsing
{
    public readonly struct NodeID
    {
        internal readonly int ID;

        internal NodeID(int id)
        {
            this.ID = id;
        }

        public override int GetHashCode() => HashCode.Combine(ID);

        public override bool Equals(object? obj) => obj is NodeID other && this.ID == other.ID;

        public override string ToString() => ID.ToString();

        public static bool operator ==(NodeID a, NodeID b) => a.ID == b.ID;
        public static bool operator !=(NodeID a, NodeID b) => a.ID != b.ID;
    }
}
