using LogicScript.Parsing.Structures;
using System;

namespace LogicScript.Interpreting
{
    internal class NotConstantException : Exception
    {
        public ICodeNode Node { get; }

        public NotConstantException(string message, ICodeNode node) : base(message)
        {
            this.Node = node;
        }
    }
}
