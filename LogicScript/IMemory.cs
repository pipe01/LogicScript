using LogicScript.Data;

namespace LogicScript
{
    public interface IMemory
    {
        int Capacity { get; }

        bool GetBit(int index);
        void SetBit(int index, bool value);

        void Set(BitsValue value);
        BitsValue Get();
    }

    public sealed class VolatileMemory : IMemory
    {
        public int Capacity { get; } = 256;

        private readonly bool[] Memory;

        public VolatileMemory()
        {
            this.Memory = new bool[Capacity];
        }

        public bool GetBit(int index) => Memory[index];

        public void SetBit(int index, bool value) => Memory[index] = value;

        public void Set(BitsValue value) => value.Bits.CopyTo(Memory, 0);

        public BitsValue Get() => new BitsValue(Memory);
    }
}
