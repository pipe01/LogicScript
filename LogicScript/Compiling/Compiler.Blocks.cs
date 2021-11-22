using LogicScript.Parsing.Structures.Blocks;

namespace LogicScript.Compiling
{
    partial struct Compiler
    {
        private void Visit(Block block)
        {
            var end = IL.DefineLabel(block.GetType().Name.ToLower() + "End");

            switch (block)
            {
                case WhenBlock whenBlock:
                    if (whenBlock.Condition != null)
                    {
                        Visit(whenBlock.Condition);
                        LoadNumber();
                        IL.Brfalse(end);
                    }

                    Visit(whenBlock.Body);
                    break;

                case StartupBlock startup:
                    IL.Ldarg(2);
                    IL.Brfalse(end);
                    Visit(startup.Body);
                    break;
            }

            IL.MarkLabel(end);
        }
    }
}