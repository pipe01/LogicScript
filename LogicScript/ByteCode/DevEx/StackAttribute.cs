using System;
using System.Collections.Generic;

namespace LogicScript.ByteCode.DevEx
{
    [AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class StackAttribute : Attribute
    {
        public IReadOnlyList<int> Amounts { get; }

        public StackAttribute(params int[] amounts)
        {
            this.Amounts = amounts;
        }
    }
}