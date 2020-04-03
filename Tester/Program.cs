using LogicScript;
using LogicScript.Data;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            const string script = @"
when 0 = 0
    # Set individual output bits
    out[2] = and(1, 1)
    out[2] = and(1, in[2])

    # Set all the output bits
    out = 1010
    out = add(10', 3')
end

# Other example case statements:
# when in = (1, 0, in[1], 1)
# when in = 12'
# when (in[0], in[1]) = 10
# when (in[2], in[1]) = 3'
";

            var result = Script.Compile(script);

            if (!result.Success)
            {
                foreach (var item in result.Errors)
                {
                    Console.WriteLine(item);
                }
                Console.ReadKey(true);
                return;
            }

            var engine = new LogicRunner(result.Script);
            var machine = new Machine();
            engine.DoUpdate(machine);

#if RELEASE
            machine.ConsoleOutput = false;

            const int iterations = 10000000;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                engine.DoUpdate(machine);
            }

            sw.Stop();
            Console.WriteLine((double)(sw.ElapsedTicks / iterations) / TimeSpan.TicksPerMillisecond + "ms per iteration");
#endif

            Console.ReadKey(true);
        }
    }

    public class Machine : IMachine
    {
        public int InputCount => Inputs.Length;
        public int OutputCount => Outputs.Length;

        private bool[] Inputs = new[] { true, false, true, false };
        private bool[] Outputs = new[] { true, false, true, false };

        public bool GetInput(int i)
        {
            var v = Inputs[i];

            Console.WriteLine($"Read input {i}: {v}");
            return v;
        }

        public BitsValue GetInputs()
        {
            Console.WriteLine("Read inputs");

            return new BitsValue(Inputs);
        }

        public void SetOutput(int i, bool on)
        {
            Outputs[i] = on;

            Console.WriteLine($"Set output {i} to {on}");
        }

        public void SetOutputs(BitsValue values)
        {
            Array.Copy(values.Bits, Outputs, OutputCount);

            Console.WriteLine($"Set outputs to {values.Number} ({values})");
        }
    }
}
