using System;
using FastExpressionCompiler.LightExpression;

namespace LogicScript.Transpiling
{
    internal static class Extensions
    {
        public static bool IsBool(this Expression t) => t.Type == typeof(bool);
    }
}