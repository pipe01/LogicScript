using GrEmit;
using LogicScript.Data;
using LogicScript.Parsing.Structures;
using System;
using System.Collections.Generic;

namespace LogicScript
{
    internal partial class CompilerVisitor
    {
        private GroboIL.Label CurrentLoopEndLabel;

        public void Visit(IReadOnlyList<Statement> statements)
        {
            foreach (var stmt in statements)
            {
                Visit(stmt);
            }
        }

        private void Visit(Statement statement)
        {
            Generator.MarkLabel(Generator.DefineLabel(statement.ToString()));

            switch (statement)
            {
                case ExpressionStatement expr:
                    Visit(expr.Expression);
                    Generator.Pop();
                    break;

                case IfStatement ifStmt:
                    Visit(ifStmt);
                    break;

                case ForStatement forStmt:
                    Visit(forStmt);
                    break;

                case WhileStatement whileStmt:
                    Visit(whileStmt);
                    break;

                case BreakStatement breakStmt:
                    Visit(breakStmt);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void Visit(IfStatement stmt)
        {
            Visit(stmt.Condition);
            BitsValueToNumber();

            if (stmt.Else == null)
            {
                var bodyEndLabel = Generator.DefineLabel("body");

                Generator.Brfalse(bodyEndLabel);

                Visit(stmt.Body);
                Generator.MarkLabel(bodyEndLabel);
            }
            else
            {
                var mainStartLabel = Generator.DefineLabel("main");
                var mainEndLabel = Generator.DefineLabel("else");

                Generator.Brtrue(mainStartLabel);

                Visit(stmt.Else);
                Generator.Br(mainEndLabel);

                Generator.MarkLabel(mainStartLabel);
                Visit(stmt.Body);
                Generator.MarkLabel(mainEndLabel);
            }
        }

        private void Visit(ForStatement stmt)
        {
            var loopStartLabel = Generator.DefineLabel("loopstart");

            var indexLocal = Local(stmt.VarName);
            var toLocal = Generator.DeclareLocal(typeof(ulong), "end");

            var previousEndLabel = CurrentLoopEndLabel;
            var endLabel = CurrentLoopEndLabel = Generator.DefineLabel("forend");

            Visit(stmt.From);
            Generator.Dup();
            Generator.Ldobj(typeof(BitsValue));
            Generator.Stloc(indexLocal);

            Visit(stmt.To);
            BitsValueToNumber();
            Generator.Stloc(toLocal);

            BitsValueToNumber();

            Generator.MarkLabel(loopStartLabel);

            Visit(stmt.Body);

            // Increment index
            Generator.Ldc_I8(1);
            Generator.Conv<ulong>();
            Generator.Add();
            NumberToBitsValue();
            Generator.Ldobj(typeof(BitsValue));
            Generator.Stloc(indexLocal);

            // Check index == max, if true branch to end
            Generator.Ldloca(indexLocal);
            BitsValueToNumber();
            Generator.Dup();
            Generator.Ldloc(toLocal);
            Generator.Bne_Un(loopStartLabel);

            Generator.MarkLabel(endLabel);
            CurrentLoopEndLabel = previousEndLabel;

            Generator.Pop();
        }

        private void Visit(WhileStatement stmt)
        {
            var previousEndLabel = CurrentLoopEndLabel;

            var loopStartLabel = Generator.DefineLabel("whilestart");
            var loopEndLabel = CurrentLoopEndLabel = Generator.DefineLabel("whileend");

            Generator.MarkLabel(loopStartLabel);

            Visit(stmt.Condition);
            BitsValueToNumber();
            Generator.Brfalse(loopEndLabel);

            Visit(stmt.Body);
            Generator.Br(loopStartLabel);

            Generator.MarkLabel(loopEndLabel);

            CurrentLoopEndLabel = previousEndLabel;
        }

        private void Visit(BreakStatement stmt)
        {
            if (CurrentLoopEndLabel == null)
                throw new LogicEngineException("Break statement outside of loop", stmt);

            Generator.Br(CurrentLoopEndLabel);
        }
    }
}
