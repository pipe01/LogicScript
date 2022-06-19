using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LogicScript.ByteCode.DevEx;

namespace LogicScript.ByteCode
{
    partial class TapeReader
    {
        private static IDictionary<OpCodes, (OpCodeAttribute, StackAttribute?)> OpCodeAttributes;

        static TapeReader()
        {
            OpCodeAttributes = new Dictionary<OpCodes, (OpCodeAttribute, StackAttribute?)>();

            foreach (var field in typeof(OpCodes).GetFields())
            {
                if (field.IsSpecialName)
                    continue;

                var opAttr = field.GetCustomAttribute<OpCodeAttribute>();
                var stackAttr = field.GetCustomAttribute<StackAttribute>();

                if (opAttr == null)
                    continue;

                var opcode = (OpCodes)field.GetValue(null);

                OpCodeAttributes[opcode] = (opAttr, stackAttr);
            }
        }

        public void Dump(TextWriter w)
        {
            var tape = Clone();
            tape.ReadHeader();

            int stack = 0;

            while (!tape.IsEOF)
            {
                int opcodePos = tape.Position;

                var opcode = tape.ReadOpCode();
                if (!OpCodeAttributes.TryGetValue(opcode, out var attrs))
                    throw new Exception($"Invalid opcode value {opcode}");

                var (opAttr, stackAttr) = attrs;

                var stackValue = stackAttr?.Amounts.Sum() ?? 0;
                stack += stackValue;

                w.Write('(');
                w.Write(new string('*', stack).PadLeft(5));
                w.Write(") ");
                w.Write(opcodePos.ToString().PadLeft(4));
                w.Write(' ');
                w.Write(opAttr.ShortName);
                w.Write(' ');

                bool isFirst = true;
                foreach (var arg in opAttr.Arguments)
                {
                    if (isFirst)
                        isFirst = false;
                    else
                        w.Write(' ');

                    w.Write(arg.Name);
                    w.Write('=');

                    var value = arg.Bytes switch
                    {
                        1 => tape.ReadByte(),
                        2 => tape.ReadUInt16(),
                        4 => tape.ReadUInt32(),
                        8 => tape.ReadUInt64(),
                        _ => throw new Exception("Invalid opcode argument length")
                    };

                    w.Write(value);
                }

                w.WriteLine();
            }
        }
    }
}