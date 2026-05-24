using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LogicScript.Data;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace LogicScript.Tests
{
    [TestFixture(TestName = "Benches")]
    public class BenchTest
    {
        static IEnumerable<TestCaseParameters> Benches
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();

                var prefix = assembly.GetName().Name + ".Benches.";
                var lsxFiles = assembly.GetManifestResourceNames().Where(n => n.EndsWith(".lsx"));

                return lsxFiles.SelectMany(lsxFile =>
                {
                    var script = ParseScript(lsxFile);

                    return script.TestCases.Select((@case, i) =>
                    {
                        var caseName = @case.Name ?? $"Case {i}";

                        return new TestCaseParameters([lsxFile, i])
                        {
                            TestName = $"{lsxFile[prefix.Length..^".lsx".Length]}.{caseName}"
                        };
                    });
                });
            }
        }

        [OneTimeSetUp]
        public void StartTest()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
        }

        [OneTimeTearDown]
        public void EndTest()
        {
            Trace.Flush();
        }

        [TestCaseSource(nameof(Benches))]
        public async Task Run(string lsbenchFile, int caseIndex)
        {
            var script = ParseScript(lsbenchFile);
            var result = await script.TestCases[caseIndex].Run(script, null);

            foreach (var line in result.PrintedLines)
            {
                Debug.WriteLine($"[Script Output] {line}");
            }

            if (!result.Success)
            {
                var msg = new StringBuilder();
                msg.AppendLine($"Failed on step {result.FailedStep!.Value.StepIndex}:");

                msg.AppendLine($"           Input: {FormatIO(result.FailedStep.Value.Inputs)}");
                msg.AppendLine($" Expected output: {FormatIO(result.FailedStep.Value.ExpectedOutputs)}");
                msg.AppendLine($"Disparate output: {FormatIO(result.FailedStep.Value.MismatchedOutputs)}");

                Assert.Fail(msg.ToString());
            }
        }

        private static Script ParseScript(string lsxFile)
        {
            var lsxSource = ReadEmbeddedFile(lsxFile);

            var (script, scriptErrors) = Script.Parse(lsxSource, lsxFile);
            Assert.NotNull(script);
            Assert.IsEmpty(scriptErrors);

            return script!;
        }

        private static string ReadEmbeddedFile(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(name);
            using var reader = new StreamReader(stream!);

            return reader.ReadToEnd();
        }

        private static string FormatIO(IDictionary<string, BitsValue> values)
        {
            return string.Join(' ', values.OrderBy(e => e.Key).Select(e => $"{e.Key}({e.Value.Number})").ToArray());
        }
    }
}
