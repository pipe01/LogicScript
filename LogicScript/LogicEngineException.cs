﻿using LogicScript.Parsing;
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

        internal LogicEngineException(string message, ICodeNode node) : this(message, node.Location)
        {
        }

        internal LogicEngineException(string message, SourceLocation location) : base(message + $" at {location}")
        {
        }

        public LogicEngineException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
