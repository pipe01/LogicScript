using System;
using LogicScript.Data;

#if USE_FAST_EXPRESSIONS
using FastExpressionCompiler.LightExpression;
using Expression = FastExpressionCompiler.LightExpression.Expression;
#else
using System.Linq.Expressions;
using Expression = System.Linq.Expressions.Expression;
#endif

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