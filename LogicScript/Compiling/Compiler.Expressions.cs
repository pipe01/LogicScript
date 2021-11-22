using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LogicScript.Data;
using LogicScript.Interpreting;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Expressions;

namespace LogicScript.Compiling
{
    partial struct Compiler
    {
        private void Visit(Expression expr)
        {
            switch (expr)
            {
                case BinaryOperatorExpression binOp:
                    Visit(binOp);
                    break;

                case NumberLiteralExpression lit:
                    Visit(lit);
                    break;
            }
        }

        private void Visit(BinaryOperatorExpression expr)
        {
            Visit(expr.Left);
            Visit(expr.Right);
            IL.Ldc_I4((int)expr.Operator);
            LoadSpan(expr.Span);
            IL.Call(typeof(Operations).GetMethod(nameof(Operations.DoOperation)));
        }

        private void Visit(NumberLiteralExpression expr)
        {
            LoadBitsValue(expr.Value);
        }
    }
}