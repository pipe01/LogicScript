namespace LogicScript
{
    internal static class Extensions
    {
        public static bool ContainsDecimalDigits(this string str)
        {
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] > '1')
                    return true;
            }

            return false;
        }
    }
}
