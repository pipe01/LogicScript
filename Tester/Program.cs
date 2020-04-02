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
    [StructLayout(LayoutKind.Explicit)]
    public struct Union
    {
        [FieldOffset(0)]
        public bool[] Bool;

        [FieldOffset(0)]
        public byte Byte;
    }

    class Program
    {
        static void Main(string[] args)
        {
            var errors = new ErrorSink();

            var u = new Union();


            bool[] a = new[] { false, false, true, true };
            bool[] b = new[] { false, true, false, true };

            var l = new Lexer(
@"
when in[1] = 0
    # Set individual output bits
    out = and(1010, 0110)
    out[2] = and(0, in[2])

    # Set all the output bits
    out = 1010
    out = 14'  #The ' denotes that it's a decimal number instead of a binary one
end

# Other example case statements:
# when in = (1, 0, in[1], 1)
# when in = 12'
# when (in[0], in[1]) = 10
# when (in[2], in[1]) = 3'
", errors);

            var ls = l.Lex().ToArray();

            if (errors.Count > 0)
            {
                foreach (var item in errors)
                {
                    Console.WriteLine(item);
                }
                Console.ReadKey(true);
                return;
            }

            Script script = new Parser(ls, errors).Parse();

            if (errors.Count > 0)
            {
                foreach (var item in errors)
                {
                    Console.WriteLine(item);
                }
                Console.ReadKey(true);
                return;
            }

            var engine = new LogicRunner(script);
            var machine = new Machine { ConsoleOutput = true };
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

        public bool ConsoleOutput { get; set; }

        public bool GetInput(int i)
        {
            var v = Inputs[i];

            if (ConsoleOutput)
                Console.WriteLine($"Read input {i}: {v}");
            return v;
        }

        public void SetOutput(int i, bool on)
        {
            Outputs[i] = on;

            if (ConsoleOutput)
                Console.WriteLine($"Set output {i} to {on}");
        }

        public void SetOutputs(BitsValue values)
        {
            values.Bits.CopyTo(Outputs);

            if (ConsoleOutput)
                Console.WriteLine($"Set outputs to ({string.Join(", ", values.Bits.ToArray())})");
        }
    }
}
