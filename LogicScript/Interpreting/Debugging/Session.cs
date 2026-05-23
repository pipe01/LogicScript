using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures.Statements;

namespace LogicScript.Interpreting.Debugging
{
    public delegate void DebuggableDelegate();

    public class Session : IDebugger
    {
        private readonly record struct Breakpoint(int Number, SourceLocation Location);

        public readonly record struct PauseState(int? BreakpointNumber, Statement Statement, Interpreter Interpreter)
        {
            public readonly TaskCompletionSource<bool> PauseBarrier = new();

            public bool HasBreakpoint => BreakpointNumber != null;
        }

        private readonly List<Breakpoint> LineBreakpoints = [];
        public PauseState? CurrentPause { get; private set; }

        public bool Attached => Paused != null;

        public event Action? Paused;

        private int BreakpointCounter = 0;
        private bool IgnoreNext;
        private bool PauseNext;

        public int AddBreakpoint(SourceLocation location)
        {
            var number = BreakpointCounter++;
            LineBreakpoints.Add(new(number, location));

            return number;
        }

        public void ClearBreakpoints()
        {
            LineBreakpoints.Clear();
        }

        public void Continue()
        {
            if (CurrentPause != null)
            {
                IgnoreNext = true;
                CurrentPause.Value.PauseBarrier.TrySetResult(true);
            }
        }

        public void Next()
        {
            if (CurrentPause != null)
            {
                PauseNext = true;
                Continue();
            }
        }

        void IDebugger.TraceStatement(Interpreter interpreter, Statement stmt, out bool pause)
        {
            pause = false;

            if (!Attached)
            {
                return;
            }

            if (IgnoreNext)
            {
                CurrentPause = null;
                IgnoreNext = false;
                return;
            }

            if (PauseNext)
            {
                PauseNext = false;

                CurrentPause = new(null, stmt, interpreter);
                Paused?.Invoke();
                pause = true;
                return;
            }

            foreach (var bp in LineBreakpoints)
            {
                if (bp.Location.FileName == stmt.Span.Start.FileName && bp.Location.Line == stmt.Span.Start.Line)
                {
                    CurrentPause = new(bp.Number, stmt, interpreter);
                    Paused?.Invoke();
                    pause = true;
                    break;
                }
            }
        }

        public async Task WaitForResumeAsync()
        {
            if (CurrentPause != null)
                await CurrentPause.Value.PauseBarrier.Task;
        }

        public void WaitForResume()
        {
            CurrentPause?.PauseBarrier.Task.Wait();
        }
    }
}
