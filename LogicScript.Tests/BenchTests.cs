using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LogicScript.Testing.Results;
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
                    var fileName = lsxFile[prefix.Length..^".lsx".Length];

                    return script.TestCases.SelectMany((@case, i) =>
                    {
                        var suffix = script.TestCases.Count == 1 ? "" : ("." + (@case.Name ?? $"Case_{i}"));

                        return new TestCaseParameters[]
                        {
                            new([lsxFile, i, false])
                            {
                                TestName = "NoDebug." + fileName + suffix
                            },
                            new([lsxFile, i, true])
                            {
                                TestName = "Debug." + fileName + suffix
                            },
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
        public async Task Run(string lsbenchFile, int caseIndex, bool debug)
        {
            using var script = ParseScript(lsbenchFile);
            var result = await script.TestCases[caseIndex].Run(script, debugger: debug ? DummyDebugger.Instance : null);

            foreach (var line in result.PrintedLines)
            {
                Debug.WriteLine($"[Script Output] {line}");
            }

            switch (result)
            {
                case FailedStepCaseResult failedStep:
                    Assert.Fail(failedStep.GetFailureString());
                    break;

                case LimitReachedCaseResult:
                    Assert.Fail("Statement limit reached");
                    break;
            }
        }

        private static Script ParseScript(string lsxFile)
        {
            var lsxSource = ReadEmbeddedFile(lsxFile);

            var (script, scriptErrors) = Script.Parse(lsxSource, lsxFile);
            Assert.IsEmpty(scriptErrors, $"Script {lsxFile} has errors");
            Assert.NotNull(script);

            return script!;
        }

        private static string ReadEmbeddedFile(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(name);
            using var reader = new StreamReader(stream!);

            return reader.ReadToEnd();
        }
    }
}
