using LogicScript.Parsing.Structures;
using System;

namespace LogicScript.Interpreting
{
    internal class NotConstantException(string message, ICodeNode node) : Exception(message)
    {
        public ICodeNode Node { get; } = node;
    }
}
