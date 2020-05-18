using LogicScript.Data;
using LogicScript.Parsing.Structures;
using System;
using System.Collections.Generic;

namespace LogicScript
{
    internal partial class CompilerVisitor
    {
        private IDictionary<string, Action<IList<Expression>>> Functions = new Dictionary<string, Action<IList<Expression>>>();

        private void RegisterFunctions()
        {
            Functions = new Dictionary<string, Action<IList<Expression>>>
            {
                ["and"] = FunctionAnd,
                ["or"] = FunctionOr,
                ["sum"] = FunctionSum,
                ["trunc"] = FunctionTruncate,
            };
        }

        private void EmitFunctionCall(string name, IList<Expression> args, ICodeNode node)
        {
            if (Functions.TryGetValue(name, out var func))
            {
                try
                {
                    func(args);
                }
                catch (LogicEngineException ex)
                {
                    throw new LogicEngineException(ex.Message, node);
                }
            }
            else
            {
                throw new LogicEngineException($"Unknown function '{name}'", node);
            }
        }

        private void LoadArguments(IList<Expression> args, bool convertToNumber = false)
        {
            foreach (var item in args)
            {
                Visit(item);

                if (convertToNumber)
                    BitsValueToNumber();
            }
        }

        private void FunctionAnd(IList<Expression> args)
        {
            if (args.Count != 1)
                throw new LogicEngineException("Function 'and' expected 1 argument");

            LoadArguments(args);

            Generator.Call(Info.OfPropertyGet<BitsValue>(nameof(BitsValue.AreAllBitsSet)));
            BoolToBitsValue();
        }
        
        private void FunctionOr(IList<Expression> args)
        {
            if (args.Count != 1)
                throw new LogicEngineException("Function 'or' expected 1 argument");

            LoadArguments(args);

            Generator.Call(Info.OfPropertyGet<BitsValue>(nameof(BitsValue.IsAnyBitSet)));
            BoolToBitsValue();
        }
        
        private void FunctionSum(IList<Expression> args)
        {
            if (args.Count != 1)
                throw new LogicEngineException("Function 'sum' expected 1 argument");

            LoadArguments(args);

            Generator.Call(Info.OfPropertyGet<BitsValue>(nameof(BitsValue.PopulationCount)));
            Generator.Conv<ulong>();
            NumberToBitsValue();
        }

        private void FunctionTruncate(IList<Expression> args)
        {
            if (args.Count == 0)
                throw new LogicEngineException("Function 'trunc' expected 1 or 2 arguments");

            Visit(args[0]);

            if (args.Count == 1)
            {
                Generator.Call(Info.OfPropertyGet<BitsValue>(nameof(BitsValue.Truncated)));
                ValueToReference();
            }
            else if (args.Count == 2)
            {
                Visit(args[1]);
                BitsValueToNumber();
                Generator.Conv<int>();

                Generator.Call(Info.OfMethod<BitsValue>(nameof(BitsValue.Truncate), "System.Int32"));
                ValueToReference();
            }
        }
    }
}
