using LogicScript.DX.CLI.Commands;
using Yaclip;

namespace LogicScript.DX.CLI
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var app = YaclipApp.New()
                .Name("LogicScript CLI")
                .ExecutableName("lscli")
                .GenerateHelpCommand(true)
                .Command<TestCommand>("test", c => c
                    .Description("Runs test benches")
                    .Callback(async o => await o.RunAsync())
                    .Argument(o => o.Files, a => a
                        .Name("files"))
                    .Option(o => o.FailFast, "fail-fast", o => o
                        .Description("Stop running tests when one fails"))
                    .Option(o => o.Debug, 'd', "debug", o => o
                        .Description("Start debugging server"))
                )
                .Build();

            await app.Run(args);
        }
    }
}