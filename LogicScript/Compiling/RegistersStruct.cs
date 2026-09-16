using System;
using System.Collections.Generic;
using System.Linq;
// using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using FastExpressionCompiler;
using FastExpressionCompiler.LightExpression;

namespace LogicScript.Compiling
{
    public interface IRegisters
    {
        /// <summary>
        /// Sum of the size in bytes of all registers.
        /// </summary>
        int Size { get; }

        void Decode(ReadOnlySpan<byte> data);
        void Encode(Span<byte> data);

        ulong GetRegister(int index, int vector);
        void SetRegister(int index, int vector, ulong value);
    }

    internal static class RegistersStruct
    {
        private record struct ComputedRegister(MachineRegister MachineRegister, FieldInfo Field, Type ItemType, int ByteSize, int ByteStart);

        public static Type Generate(MachineRegister[] registers)
        {
            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("<>RegistersAssembly"), AssemblyBuilderAccess.Run);
            var mb = ab.DefineDynamicModule("Module");
            var tb = mb.DefineType("RegistersStruct", TypeAttributes.Class);
            tb.AddInterfaceImplementation(typeof(IRegisters));

            var computedRegisters = new List<ComputedRegister>();

            int totalBytes = 0;
            for (int i = 0; i < registers.Length; i++)
            {
                var reg = registers[i];
                var (type, size) = GetRegisterSize(reg);

                var field = tb.DefineField($"Register{i}", reg.VectorLength > 1 ? type.MakeArrayType() : type, FieldAttributes.Public);
                computedRegisters.Add(new(reg, field, type, size, totalBytes));

                totalBytes += size * reg.VectorLength;
            }

            var ctorMethod = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, Type.EmptyTypes);
            GenerateConstructorMethod(ctorMethod);

            var decodeMethod = tb.DefineMethod(
                nameof(IRegisters.Decode),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                typeof(void),
                [typeof(ReadOnlySpan<byte>)]
            );
            GenerateDecodeMethod(decodeMethod);

            var encodeMethod = tb.DefineMethod(
                nameof(IRegisters.Encode),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                typeof(void),
                [typeof(Span<byte>)]
            );
            GenerateEncodeMethod(encodeMethod);

            var sizeProperty = tb.DefineProperty(nameof(IRegisters.Size), PropertyAttributes.None, typeof(int), Type.EmptyTypes);
            var getSizeMethod = tb.DefineMethod(
                "get_" + nameof(IRegisters.Size),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                typeof(int),
                Type.EmptyTypes
            );
            Expression.Lambda(Expression.Constant(totalBytes)).CompileFastToIL(getSizeMethod.GetILGenerator());
            sizeProperty.SetGetMethod(getSizeMethod);

            var getRegisterMethod = tb.DefineMethod(
                nameof(IRegisters.GetRegister),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                typeof(ulong),
                [typeof(int), typeof(int)]
            );
            GenerateGetRegisterMethod(getRegisterMethod);

            var setRegisterMethod = tb.DefineMethod(
                nameof(IRegisters.SetRegister),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                typeof(void),
                [typeof(int), typeof(int), typeof(ulong)]
            );
            GenerateSetRegisterMethod(setRegisterMethod);

            return tb.CreateType();

            void GenerateConstructorMethod(ConstructorBuilder builder)
            {
                // Initialize all vector registers with empty arrays

                var il = builder.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));

                var thisParam = Expression.Parameter(tb, "this");

                var block = Expression.Block(
                    typeof(void),
                    computedRegisters
                        .Where(reg => reg.MachineRegister.VectorLength > 1)
                        .Select(reg =>
                            Expression.Assign(
                                Expression.Field(
                                    thisParam,
                                    reg.Field
                                ),
                                Expression.NewArrayBounds(reg.ItemType, [Expression.Constant(reg.MachineRegister.VectorLength)])
                            )
                        )
                        .ToArray()
                );

                Expression.Lambda(block, [thisParam]).CompileFastToIL(il);
            }

            void GenerateDecodeMethod(MethodBuilder builder)
            {
                var thisParam = Expression.Parameter(tb, "this");
                var dataParam = Expression.Parameter(typeof(ReadOnlySpan<byte>), "data");

                var block = Expression.IfThen(
                    Expression.GreaterThanOrEqual(
                        Expression.Property(
                            dataParam,
                            typeof(ReadOnlySpan<byte>).GetProperty("Length")
                        ),
                        Expression.Constant(totalBytes)
                    ),
                    Expression.Block(computedRegisters.Select(reg =>
                    {
                        if (reg.MachineRegister.VectorLength == 1)
                        {
                            //= this.Register = data.Read(reg.ByteStart);
                            return Expression.Assign(
                                Expression.Field(thisParam, reg.Field),
                                SpanReadInteger(dataParam, reg.ItemType, Expression.Constant(reg.ByteStart))
                            );
                        }

                        // Vectored register

                        //= for (var i = 0; i < reg.VectorLength; i++) {
                        //=     this.Register[i] = data.Read(reg.ByteStart + reg.ByteSize * i);
                        //= }
                        return ForLoop(Expression.Constant(0), Expression.Constant(reg.MachineRegister.VectorLength), i =>
                        {
                            return Expression.Assign(
                                Expression.ArrayAccess(
                                    Expression.Field(thisParam, reg.Field),
                                    i
                                ),
                                SpanReadInteger(
                                    dataParam,
                                    reg.ItemType,
                                    Expression.Add(
                                        Expression.Constant(reg.ByteStart),
                                        Expression.Multiply(
                                            Expression.Constant(reg.ByteSize),
                                            i
                                        )
                                    )
                                )
                            );
                        });
                    }).ToArray())
                );

                Expression.Lambda(block, [thisParam, dataParam]).CompileFastToIL(builder.GetILGenerator());
            }

            void GenerateEncodeMethod(MethodBuilder builder)
            {
                var thisParam = Expression.Parameter(tb, "this");
                var dataParam = Expression.Parameter(typeof(Span<byte>), "data");

                var block = Expression.IfThen(
                    Expression.GreaterThanOrEqual(
                        Expression.Property(
                            dataParam,
                            typeof(Span<byte>).GetProperty("Length")
                        ),
                        Expression.Constant(totalBytes)
                    ),
                    Expression.Block(computedRegisters.Select(reg =>
                    {
                        if (reg.MachineRegister.VectorLength == 1)
                        {
                            //= data.Write(reg.ByteStart, this.Register);
                            return SpanWriteInteger(dataParam, reg.ItemType, Expression.Constant(reg.ByteStart), Expression.Field(thisParam, reg.Field));
                        }

                        // Vectored register

                        //= for (var i = 0; i < reg.VectorLength; i++) {
                        //=     data.Write(reg.ByteStart + reg.ByteSize * i, this.Register[i]);
                        //= }
                        return ForLoop(Expression.Constant(0), Expression.Constant(reg.MachineRegister.VectorLength), i =>
                        {
                            return SpanWriteInteger(
                                dataParam,
                                reg.ItemType,
                                Expression.Add(
                                    Expression.Constant(reg.ByteStart),
                                    Expression.Multiply(
                                        Expression.Constant(reg.ByteSize),
                                        i
                                    )
                                ),
                                Expression.ArrayAccess(
                                    Expression.Field(thisParam, reg.Field),
                                    i
                                )
                            );
                        });
                    }).ToArray())
                );

                Expression.Lambda(block, [thisParam, dataParam]).CompileFastToIL(builder.GetILGenerator());
            }

            void GenerateGetRegisterMethod(MethodBuilder builder)
            {
                var thisParam = Expression.Parameter(tb, "this");
                var indexParam = Expression.Parameter(typeof(int), "index");
                var vectorParam = Expression.Parameter(typeof(int), "vector");

                var block = computedRegisters.Aggregate(
                    (Expression)Expression.Constant(0UL),
                    (acum, reg) => Expression.Condition(
                        Expression.Equal(
                            indexParam,
                            Expression.Constant(reg.ByteStart)
                        ),
                        Expression.Convert(
                            reg.MachineRegister.VectorLength == 1
                                ? Expression.Field(thisParam, reg.Field)
                                : Expression.ArrayAccess(
                                    Expression.Field(thisParam, reg.Field),
                                    vectorParam
                                ),
                            typeof(ulong)
                        ),
                        acum
                    )
                );

                Expression.Lambda(block, [thisParam, indexParam, vectorParam]).CompileFastToIL(builder.GetILGenerator());
            }

            void GenerateSetRegisterMethod(MethodBuilder builder)
            {
                var thisParam = Expression.Parameter(tb, "this");
                var indexParam = Expression.Parameter(typeof(int), "index");
                var vectorParam = Expression.Parameter(typeof(int), "vector");
                var valueParam = Expression.Parameter(typeof(ulong), "value");

                var block = computedRegisters.Aggregate(
                    (Expression)Expression.Empty(),
                    (acum, reg) => Expression.IfThenElse(
                        Expression.Equal(
                            indexParam,
                            Expression.Constant(reg.ByteStart)
                        ),
                        Expression.Assign(
                            reg.MachineRegister.VectorLength == 1
                                ? Expression.Field(thisParam, reg.Field)
                                : Expression.ArrayAccess(
                                    Expression.Field(thisParam, reg.Field),
                                    vectorParam
                                ),
                            Expression.Convert(
                                valueParam,
                                reg.ItemType
                            )
                        ),
                        acum
                    )
                );

                Expression.Lambda(block, [thisParam, indexParam, vectorParam, valueParam]).CompileFastToIL(builder.GetILGenerator());
            }
        }

        public static (Type Type, int Size) GetRegisterSize(MachineRegister reg)
        {
            return reg.BitSize switch
            {
                <= 8 => (typeof(byte), 1),
                <= 16 => (typeof(ushort), 2),
                <= 32 => (typeof(uint), 4),
                <= 64 => (typeof(ulong), 8),
                _ => throw new Exception($"Too large bit size {reg.BitSize}")
            };
        }

        // Helper methods for reading and writing spans since we can't express Span indexing using System.Linq.Expressions
        internal static byte ReadByte(ReadOnlySpan<byte> span, int index) => span[index];
        internal static void WriteByte(Span<byte> span, int index, byte value) => span[index] = value;
        private static readonly MethodInfo ReadByteMethod = typeof(RegistersStruct).GetMethod(nameof(ReadByte), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo WriteByteMethod = typeof(RegistersStruct).GetMethod(nameof(WriteByte), BindingFlags.NonPublic | BindingFlags.Static);


        // We need this helper method because the MemoryMarshal.Write function takes in a "ref" of the value, which we can't do with expressions
        internal static void WriteItem<T>(Span<byte> span, int start, T value) where T : struct
            => MemoryMarshal.Write(span[start..], ref value);
        private static readonly MethodInfo WriteItemMethod = typeof(RegistersStruct).GetMethod(nameof(WriteItem), BindingFlags.NonPublic | BindingFlags.Static);

        // I don't know why we need this one, but it's nicer
        internal static T ReadItem<T>(Span<byte> span, int start) where T : struct
            => MemoryMarshal.Read<T>(span[start..]);
        private static readonly MethodInfo ReadItemMethod = typeof(RegistersStruct).GetMethod(nameof(ReadItem), BindingFlags.NonPublic | BindingFlags.Static);

        private static Expression SpanReadInteger(Expression readonlySpan, Type intType, Expression start)
        {
            if (intType == typeof(byte))
                return Expression.Call(ReadByteMethod, readonlySpan, start);

            return Expression.Call(
                ReadItemMethod.MakeGenericMethod(intType),
                readonlySpan, start
            );
        }
        private static Expression SpanWriteInteger(Expression span, Type intType, Expression start, Expression value)
        {
            if (intType == typeof(byte))
                return Expression.Call(WriteByteMethod, span, start, value);

            return Expression.Call(
                WriteItemMethod.MakeGenericMethod(intType),
                span, start, value
            );
        }

        private static Expression ForLoop(Expression start, Expression end, Func<Expression, Expression> body)
        {
            var loopVar = Expression.Variable(typeof(int), "i");
            var breakLabel = Expression.Label("loopBreak");

            var loop = Expression.Block(
                [loopVar],
                Expression.Assign(loopVar, start),
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.LessThan(loopVar, end),
                        Expression.Block(
                            body(loopVar),
                            Expression.PostIncrementAssign(loopVar)
                        ),
                        Expression.Break(breakLabel)
                    ),
                    breakLabel
                )
            );

            return loop;
        }
    }
}
