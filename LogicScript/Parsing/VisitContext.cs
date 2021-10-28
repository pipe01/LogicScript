﻿using LogicScript.Parsing.Structures.Expressions;
using System.Collections.Generic;

namespace LogicScript.Parsing
{
    internal sealed class VisitContext
    {
        public Script Script { get; }
        public IDictionary<string, Expression> Constants { get; } = new Dictionary<string, Expression>();

        public VisitContext(Script script)
        {
            this.Script = script;
        }

        public bool DoesIdentifierExist(string iden)
            => Script.Inputs.ContainsKey(iden)
            || Script.Outputs.ContainsKey(iden)
            || Script.Registers.ContainsKey(iden);
    }
}
