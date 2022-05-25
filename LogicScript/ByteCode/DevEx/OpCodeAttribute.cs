using System;
using System.Collections.Generic;

namespace LogicScript.ByteCode.DevEx
{
    [AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class OpCodeAttribute : Attribute
    {
        public string ShortName { get; }
        public IReadOnlyList<(string Name, int Bytes)> Arguments { get; }

        public OpCodeAttribute(string shortName, params object[] arguments)
        {
            if (arguments.Length % 2 != 0)
                throw new Exception("Invalid opcode attribute");

            this.ShortName = shortName;

            var args = new List<(string, int)>();
            for (int i = 0; i < arguments.Length; i += 2)
            {
                args.Add(((string)arguments[i], (int)arguments[i+1]));
            }

            this.Arguments = args;
        }
    }
}