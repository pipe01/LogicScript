namespace LogicScript.Utils
{
    internal static class BitUtils
    {
        public static int GetBitSize(ulong num)
        {
            int len = 0;

            while (num != 0)
            {
                num >>= 1;
                len++;
            }

            return len;
        }
    }
}
