using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using LogicScript.Parsing.Structures;
using Sigil.NonGeneric;

namespace LogicScript.Compiling
{
    internal class RegistersStructBuilder
    {
        public record struct ComputedRegister(MachinePortInfo PortInfo, FieldInfo Field, Type ItemType, int ItemByteSize, int ByteStart, int Index)
        {
            public readonly bool IsVector => PortInfo.VectorLength > 1;
        }

        public readonly Dictionary<MachinePortInfo, ComputedRegister> Registers = [];
        private readonly int TotalBytes;
        private readonly TypeBuilder TypeBuilder;

        public RegistersStructBuilder(TypeBuilder tb, MachinePortInfo[] registers)
        {
            TypeBuilder = tb;

            for (int i = 0; i < registers.Length; i++)
            {
                var reg = registers[i];
                var (type, size) = GetRegisterSize(reg);

                var field = TypeBuilder.DefineField($"Register{i}", reg.VectorLength > 1 ? type.MakeArrayType() : type, FieldAttributes.Public);
                Registers.Add(reg, new(reg, field, type, size, TotalBytes, i));

                TotalBytes += size * reg.VectorLength;
            }
        }

        public void EmitConstructorInit(Emit emitter)
        {
            // Initialize all vector registers with empty arrays

            foreach (var reg in Registers.Values.Where(reg => reg.IsVector))
            {
                emitter.LoadArgument(0);
                emitter.LoadConstant(reg.PortInfo.VectorLength);
                emitter.NewArray(reg.ItemType);
                emitter.StoreField(reg.Field);
            }
        }

        public void GenerateMethods()
        {
            var decodeEmitter = Emit.BuildInstanceMethod(
                typeof(void),
                [typeof(ReadOnlySpan<byte>)],
                TypeBuilder,
                nameof(IRegistersInstance.DecodeRegisters),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                doVerify: false
            );
            GenerateDecodeMethod(decodeEmitter);

            var encodeMethod = Emit.BuildInstanceMethod(
                typeof(void),
                [typeof(Span<byte>)],
                TypeBuilder,
                nameof(IRegistersInstance.EncodeRegisters),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot
            );
            GenerateEncodeMethod(encodeMethod);

            var sizeProperty = TypeBuilder.DefineProperty(nameof(IRegistersInstance.RegistersSize), PropertyAttributes.None, typeof(int), Type.EmptyTypes);
            var getSizeEmitter = Emit.BuildInstanceMethod(
                typeof(int),
                Type.EmptyTypes,
                TypeBuilder,
                "get_" + nameof(IRegistersInstance.RegistersSize),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot
            );
            getSizeEmitter.LoadConstant(TotalBytes);
            getSizeEmitter.Return();
            sizeProperty.SetGetMethod(getSizeEmitter.CreateMethod());

            var getRegisterEmitter = Emit.BuildInstanceMethod(
                typeof(ulong),
                [typeof(int), typeof(int)],
                TypeBuilder,
                nameof(IRegistersInstance.GetRegister),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot
            );
            GenerateGetRegisterMethod(getRegisterEmitter);

            var setRegisterEmitter = Emit.BuildInstanceMethod(
                typeof(void),
                [typeof(int), typeof(int), typeof(ulong)],
                TypeBuilder,
                nameof(IRegistersInstance.SetRegister),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                doVerify: false
            );
            GenerateSetRegisterMethod(setRegisterEmitter);

            var resetMethodEmitter = Emit.BuildInstanceMethod(
                typeof(void),
                Type.EmptyTypes,
                TypeBuilder,
                nameof(IRegistersInstance.ResetRegisters),
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                doVerify: false
            );
            GenerateResetMethod(resetMethodEmitter);

            void GenerateDecodeMethod(Emit emitter)
            {
                var exit = emitter.DefineLabel();

                //= if (span.Length < TotalBytes) return;
                emitter.LoadArgumentAddress(1);
                emitter.Call(typeof(ReadOnlySpan<byte>).GetMethod("get_Length"));
                emitter.LoadConstant(TotalBytes);
                emitter.BranchIfLess(exit);

                foreach (var reg in Registers.Values)
                {
                    if (reg.IsVector)
                    {
                        /*=
                        Span<TItem> target = new Span<TItem>(this.Field);
                        ReadOnlySpan<byte> view = data.Slice(byteStart);
                        ReadOnlySpan<TItem> casted = MemoryMarshal.Cast<byte, TItem>(view);
                        casted.CopyTo(target);
                        */

                        using var casted = emitter.DeclareLocal(typeof(ReadOnlySpan<>).MakeGenericType(reg.ItemType));

                        emitter.LoadArgumentAddress(1);
                        emitter.LoadConstant(reg.ByteStart);
                        emitter.LoadConstant(reg.ItemByteSize * reg.PortInfo.VectorLength);
                        emitter.Call(typeof(ReadOnlySpan<byte>).GetMethod(nameof(ReadOnlySpan<>.Slice), [typeof(int), typeof(int)]));
                        emitter.Call(typeof(MemoryMarshal).GetMethod(nameof(MemoryMarshal.Cast), [typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0))]).MakeGenericMethod(typeof(byte), reg.ItemType));
                        emitter.StoreLocal(casted);

                        emitter.LoadLocalAddress(casted);
                        emitter.LoadArgument(0);
                        emitter.LoadField(reg.Field);
                        emitter.Call(typeof(MemoryExtensions).GetMethod(nameof(MemoryExtensions.AsSpan), [Type.MakeGenericMethodParameter(0).MakeArrayType()]).MakeGenericMethod(reg.ItemType));
                        emitter.Call(typeof(ReadOnlySpan<>).MakeGenericType(reg.ItemType).GetMethod(nameof(ReadOnlySpan<>.CopyTo)));
                    }
                    else
                    {
                        //= this.Field = MemoryMarshal.Read<TItem>(data.Slice(byteStart));

                        emitter.LoadArgument(0);
                        emitter.LoadArgumentAddress(1);
                        emitter.LoadConstant(reg.ByteStart);
                        emitter.Call(typeof(ReadOnlySpan<byte>).GetMethod(nameof(ReadOnlySpan<>.Slice), [typeof(int)]));
                        emitter.Call(typeof(MemoryMarshal).GetMethod(nameof(MemoryMarshal.Read)).MakeGenericMethod(reg.ItemType));
                        emitter.StoreField(reg.Field);
                    }
                }

                emitter.MarkLabel(exit);
                emitter.Return();
                emitter.CreateMethod(out var inst);
                Debug.WriteLine(inst);
            }

            void GenerateEncodeMethod(Emit emitter)
            {
                var exit = emitter.DefineLabel();

                //= if (span.Length < TotalBytes) return;
                emitter.LoadArgumentAddress(1);
                emitter.Call(typeof(Span<byte>).GetMethod("get_Length"));
                emitter.LoadConstant(TotalBytes);
                emitter.BranchIfLess(exit);

                foreach (var reg in Registers.Values)
                {
                    if (reg.IsVector)
                    {
                        /*=
                        Span<TItem> source = new Span<TItem>(this.Field);
                        ReadOnlySpan<byte> casted = MemoryMarshal.Cast<int, byte>(this.Field);
                        Span<byte> target = data.Slice(byteStart)
                        casted.CopyTo(target);
                        */

                        using var casted = emitter.DeclareLocal(typeof(ReadOnlySpan<byte>));

                        emitter.LoadArgument(0);
                        emitter.LoadField(reg.Field);
                        emitter.NewObject(typeof(ReadOnlySpan<>).MakeGenericType(reg.ItemType), [reg.ItemType.MakeArrayType()]);
                        emitter.Call(typeof(MemoryMarshal).GetMethod(nameof(MemoryMarshal.Cast), [typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0))]).MakeGenericMethod(reg.ItemType, typeof(byte)));
                        emitter.StoreLocal(casted);

                        emitter.LoadLocalAddress(casted);
                        emitter.LoadArgumentAddress(1);
                        emitter.LoadConstant(reg.ByteStart);
                        emitter.LoadConstant(reg.ItemByteSize * reg.PortInfo.VectorLength);
                        emitter.Call(typeof(Span<byte>).GetMethod(nameof(Span<>.Slice), [typeof(int), typeof(int)]));
                        emitter.Call(typeof(ReadOnlySpan<byte>).GetMethod(nameof(ReadOnlySpan<>.CopyTo)));
                    }
                    else
                    {
                        //= MemoryMarshal.Write(data.Slice(byteStart), this.Field);

                        emitter.LoadArgumentAddress(1);
                        emitter.LoadConstant(reg.ByteStart);
                        emitter.Call(typeof(Span<byte>).GetMethod(nameof(Span<>.Slice), [typeof(int)]));
                        emitter.LoadArgument(0);
                        emitter.LoadFieldAddress(reg.Field);
                        emitter.Call(typeof(MemoryMarshal).GetMethod(nameof(MemoryMarshal.Write)).MakeGenericMethod(reg.ItemType));
                    }
                }

                emitter.MarkLabel(exit);
                emitter.Return();
                emitter.CreateMethod();
            }

            void GenerateGetRegisterMethod(Emit emitter)
            {
                foreach (var reg in Registers.Values)
                {
                    var next = emitter.DefineLabel();

                    emitter.LoadConstant(reg.Index);
                    emitter.LoadArgument(1);
                    emitter.UnsignedBranchIfNotEqual(next);

                    if (reg.IsVector)
                    {
                        emitter.LoadArgument(0);
                        emitter.LoadField(reg.Field);
                        emitter.LoadArgument(2);
                        emitter.LoadElement(reg.ItemType);
                    }
                    else
                    {
                        emitter.LoadArgument(0);
                        emitter.LoadField(reg.Field);
                    }
                    emitter.Convert<ulong>();
                    emitter.Return();

                    emitter.MarkLabel(next);
                }

                emitter.NewObject<IndexOutOfRangeException>();
                emitter.Throw();
                emitter.CreateMethod();
            }

            void GenerateSetRegisterMethod(Emit emitter)
            {
                foreach (var reg in Registers.Values)
                {
                    var next = emitter.DefineLabel();

                    emitter.LoadConstant(reg.Index);
                    emitter.LoadArgument(1);
                    emitter.UnsignedBranchIfNotEqual(next);

                    if (reg.IsVector)
                    {
                        emitter.LoadArgument(0);
                        emitter.LoadField(reg.Field);
                        emitter.LoadArgument(2);
                        emitter.LoadArgument(3);
                        emitter.Convert(reg.ItemType);
                        emitter.StoreElement(reg.ItemType);
                    }
                    else
                    {
                        emitter.LoadArgument(0);
                        emitter.LoadArgument(3);
                        emitter.Convert(reg.ItemType);
                        emitter.StoreField(reg.Field);
                    }
                    emitter.Return();

                    emitter.MarkLabel(next);
                }

                emitter.NewObject<IndexOutOfRangeException>();
                emitter.Throw();
                emitter.CreateMethod();
            }

            void GenerateResetMethod(Emit emitter)
            {
                foreach (var reg in Registers.Values)
                {
                    if (reg.PortInfo.VectorLength == 1)
                    {
                        emitter.LoadArgument(0);
                        switch (reg.ItemByteSize)
                        {
                            case 1 or 2 or 4:
                                emitter.LoadConstant(0);
                                break;
                            case 8:
                                emitter.LoadConstant(0UL);
                                break;
                        }
                        emitter.StoreField(reg.Field);
                    }
                    else
                    {
                        emitter.LoadArgument(0);
                        emitter.LoadField(reg.Field);
                        switch (reg.ItemByteSize)
                        {
                            case 1 or 2 or 4:
                                emitter.LoadConstant(0);
                                break;
                            case 8:
                                emitter.LoadConstant(0UL);
                                break;
                        }
                        emitter.Call(
                            typeof(Array).GetMethod(nameof(Array.Fill), [
                                Type.MakeGenericMethodParameter(0).MakeArrayType(),
                                Type.MakeGenericMethodParameter(0)
                            ]).MakeGenericMethod(reg.ItemType)
                        );
                    }
                }

                emitter.Return();
                emitter.CreateMethod();
            }
        }

        public static (Type Type, int Size) GetRegisterSize(MachinePortInfo reg)
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
    }
}
