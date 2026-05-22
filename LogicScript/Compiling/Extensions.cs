using System;
using LogicScript.Data;
using System.Linq.Expressions;

namespace LogicScript.Compiling
{
    internal static class Extensions
    {
        public static bool IsBool(this Expression t) => t.Type == typeof(bool);

        public static void AssertBool(this Expression t)
        {
            if (!t.IsBool())
                throw new InvalidOperationException($"Expected boolean expression but got {t.Type}");
        }

        public static void AssertBits(this Expression t)
        {
            if (!typeof(BitsValue).IsAssignableFrom(t.Type))
                throw new InvalidOperationException($"Expected BitsValue expression but got {t.Type}");
        }
    }
}