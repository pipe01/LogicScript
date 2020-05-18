using GrEmit;
using LogicScript.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace LogicScript
{
    internal partial class CompilerVisitor
    {
        private static readonly ConstructorInfo BitsValueCtor = typeof(BitsValue).GetConstructor(new[] { typeof(uint) });
        private static readonly ConstructorInfo BitsValueCtorLength = typeof(BitsValue).GetConstructor(new[] { typeof(uint), typeof(int) });

        private readonly GroboIL Generator;
        private readonly ILGenerator RawGenerator;
        private readonly IDictionary<string, GroboIL.Local> Locals = new Dictionary<string, GroboIL.Local>();
        private readonly GroboIL.Local Temp1, Temp2;

        public CompilerVisitor(GroboIL generator)
        {
            this.Generator = generator ?? throw new ArgumentNullException(nameof(generator));
            this.RawGenerator = (ILGenerator)typeof(GroboIL).GetField("il", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(generator);

            Temp1 = generator.DeclareLocal(typeof(BitsValue), "temp1");
            Temp2 = generator.DeclareLocal(typeof(ulong), "temp2");

            RegisterFunctions();
        }

        private GroboIL.Local Local(string name)
        {
            if (!Locals.TryGetValue(name, out var local))
            {
                Locals[name] = local = Generator.DeclareLocal(typeof(BitsValue), name);
            }

            return local;
        }

        private void LoadMachine() => Generator.Ldarg(0);

        private void LoadMemory() => Generator.Call(Info.OfPropertyGet<IMachine>(nameof(IMachine.Memory)));

        private void LoadValue(BitsValue value)
        {
            Generator.Ldc_I8((long)value.Number);
            Generator.Conv<ulong>();
            Generator.Ldc_I4(value.Length);
            NumberToBitsValue(true);
        }

        private void PointerToValue() => Generator.Ldobj(typeof(BitsValue));

        private void ValueToReference()
        {
            var local = Generator.DeclareLocal(typeof(BitsValue), "pointer");
            Generator.Stloc(local);
            Generator.Ldloca(local);
        }

        private void ValueLength() => Generator.Ldfld(Info.OfField<BitsValue>(nameof(BitsValue.Length)));

        private void NumberToBitsValue(bool takeLength = false)
        {
            Generator.Newobj(takeLength ? BitsValueCtorLength : BitsValueCtor);
            ValueToReference();
        }

        private void BitsValueToNumber() => Generator.Ldfld(Info.OfField<BitsValue>(nameof(BitsValue.Number)));

        private void BoolToBitsValue()
        {
            Generator.Call(typeof(BitsValue).GetMethod(nameof(BitsValue.FromBool)));
            ValueToReference();
        }
    }
}
