using System;
using System.Reflection;
using LogicScript.Data;
using FastExpressionCompiler;
using System.Reflection.Emit;


#if USE_FAST_EXPRESSIONS
using FastExpressionCompiler.LightExpression;
#else
using System.Linq.Expressions;
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

        public static PropertyBuilder DefineProperty(this TypeBuilder tb, string name, Type type, FieldInfo field, bool emitSetter, MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot)
        {
            var property = tb.DefineProperty(name, PropertyAttributes.None, type, Type.EmptyTypes);

            var getterMethod = tb.DefineMethod(
                "get_" + name,
                attributes,
                type,
                Type.EmptyTypes
            );

            var getterIL = getterMethod.GetILGenerator();
            getterIL.Emit(OpCodes.Ldarg_0);
            getterIL.Emit(OpCodes.Ldfld, field);
            getterIL.Emit(OpCodes.Ret);
            property.SetGetMethod(getterMethod);

            if (emitSetter)
            {
                var setterMethod = tb.DefineMethod(
                    "set_" + name,
                    attributes,
                    typeof(void),
                    [type]
                );

                var setterIL = setterMethod.GetILGenerator();
                setterIL.Emit(OpCodes.Ldarg_0);
                setterIL.Emit(OpCodes.Ldarg_1);
                setterIL.Emit(OpCodes.Stfld, field);
                setterIL.Emit(OpCodes.Ret);
                property.SetGetMethod(setterMethod);
            }

            return property;
        }
    }
}