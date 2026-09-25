using System;
using System.Reflection;
using System.Reflection.Emit;

namespace LogicScript.Compiling
{
    internal static class Extensions
    {
        public static PropertyBuilder DefineProperty(this TypeBuilder tb, string name, Type type, FieldInfo field, bool emitSetter, Type? boxType = null, MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot)
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
            if (boxType != null)
                getterIL.Emit(OpCodes.Box, boxType);
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