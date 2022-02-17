using LogicScript.DX.CLI.Commands;
using Yaclip;

namespace LogicScript.DX.CLI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var app = YaclipApp.New()
                .Name("LogicScript CLI")
                .ExecutableName("lscli")
                .GenerateHelpCommand(true)
                .Command<TestCommand>("test", c => c
                    .Description("Runs test benches")
                    .Callback(o => o.Run())
                    .Argument(o => o.Files, a => a
                        .Name("files"))
                    .Option(o => o.FailFast, "fail-fast", o => o
                        .Description("Stop running tests when one fails")))
                .Build();

            app.Run(args);
        }
    }
}