using LogicScript.Parsing.Structures;
using System;

namespace LogicScript
{
    public class LogicEngineException : Exception
    {
        public LogicEngineException() : base()
        {
        }

        public LogicEngineException(string message) : base(message)
        {
        }
        
        internal LogicEngineException(string message, ICodeNode node) : base(message + $" at {node.Location}")
        {
        }

        public LogicEngineException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
