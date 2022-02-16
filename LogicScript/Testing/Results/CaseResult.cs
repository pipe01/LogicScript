using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogicScript.Data;

namespace LogicScript.Testing.Results
{
    public readonly struct FailedStep
    {
        public int StepIndex { get; }
        public IDictionary<string, BitsValue> Inputs { get; }
        public IDictionary<string, BitsValue> ExpectedOutputs { get; }
        public IDictionary<string, BitsValue> MismatchedOutputs { get; }

        internal FailedStep(int stepIndex, IDictionary<string, BitsValue> inputs, IDictionary<string, BitsValue> expectedOutputs, IDictionary<string, BitsValue> mismatchedOutputs)
        {
            this.StepIndex = stepIndex;
            this.Inputs = inputs;
            this.ExpectedOutputs = expectedOutputs;
            this.MismatchedOutputs = mismatchedOutputs;
        }
    }

    public class CaseResult
    {
        public string Name { get; }
        public int StepCount { get; }

        public bool Success { get; }
        public FailedStep? FailedStep { get; }

        internal CaseResult(string name, int stepCount)
        {
            this.Name = name;
            this.StepCount = stepCount;
            this.Success = true;
        }

        internal CaseResult(string name, int stepCount, FailedStep? failedStep) : this(name, stepCount)
        {
            this.FailedStep = failedStep;
            this.Success = false;
        }
    }
}