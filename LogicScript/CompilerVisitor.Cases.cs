using LogicScript.Parsing.Structures;

namespace LogicScript
{
    internal partial class CompilerVisitor
    {
        public void Visit(Case c)
        {
            switch (c)
            {
                case ConditionalCase cond:
                    Visit(cond);
                    break;

                case UnconditionalCase _:
                case OnceCase _:
                    Visit(c.Statements);
                    break;
            }
        }

        private void Visit(ConditionalCase condCase)
        {
            Visit(new IfStatement(condCase.Condition, condCase.Statements, null, condCase.Location));
        }
    }
}
