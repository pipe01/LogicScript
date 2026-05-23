using System.Net;
using System.Net.Sockets;
using LogicScript.Data;
using LogicScript.Interpreting;
using LogicScript.Interpreting.Debugging;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures.Statements;
using OmniSharp.Extensions.DebugAdapter.Protocol.Events;
using OmniSharp.Extensions.DebugAdapter.Protocol.Models;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;
using OmniSharp.Extensions.DebugAdapter.Server;

namespace LogicScript.DX.DAP;

public class LogicScriptDebugger : IDebugger, IAttachHandler, IDisconnectHandler, ISetBreakpointsHandler, IThreadsHandler, IStackTraceHandler, IScopesHandler, IVariablesHandler, IContinueHandler, INextHandler, IEvaluateHandler, IStepInHandler
{
    private TaskCompletionSource<bool> SessionDone = new();

    private bool Attached;

    private Interpreter EnsureInterpreter => CurrentPause?.Interpreter ?? throw new InvalidOperationException("Not currently paused");

    private DebugAdapterServer? Server;

    private LogicScriptDebugger()
    {
    }

    public async Task RunAsync(Stream input, Stream output)
    {
        SessionDone = new();

        ClearBreakpoints();

        Server = DebugAdapterServer.Create(opts => opts
            .WithInput(input)
            .WithOutput(output)
            .WithUnhandledExceptionHandler(_ => SessionDone.TrySetResult(false))
            .AddHandler(this)
        );

        await Server.Initialize(CancellationToken.None);

        if (CurrentPause != null)
            Pause(CurrentPause.Value);

        await SessionDone.Task;

        Continue();
    }

    #region Debugger

    private readonly record struct Breakpoint(int Number, SourceLocation Location);

    private readonly record struct PauseState(int? BreakpointNumber, Statement Statement, Interpreter Interpreter)
    {
        public readonly TaskCompletionSource<bool> PauseBarrier = new();

        public bool HasBreakpoint => BreakpointNumber != null;
    }

    private readonly List<Breakpoint> LineBreakpoints = [];
    private PauseState? CurrentPause;

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

            Pause(new(null, stmt, interpreter));
            pause = true;
            return;
        }

        foreach (var bp in LineBreakpoints)
        {
            if (bp.Location.FileName == stmt.Span.Start.FileName && bp.Location.Line == stmt.Span.Start.Line)
            {
                Pause(new(bp.Number, stmt, interpreter));
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

    #endregion

    public async Task RunSocketAsync(int port)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);

        listener.Start();

        while (true)
        {
            using var socket = await listener.AcceptSocketAsync();
            using var stream = new NetworkStream(socket);

            await RunAsync(stream, stream);
        }
    }

    public static IDebugger Launch(int port = 43532)
    {
        var debugger = new LogicScriptDebugger();

        _ = Task.Run(async () => await debugger.RunSocketAsync(port));

        return debugger;
    }

    public static async Task<IDebugger> LaunchAndWaitForAttachedAsync(int port = 43532)
    {
        var debugger = new LogicScriptDebugger();

        _ = Task.Run(async () => await debugger.RunSocketAsync(port));

        await debugger.WaitForAttachedAsync();

        return debugger;
    }

    public async Task WaitForAttachedAsync()
    {
        while (!Attached)
            await Task.Delay(100); // This is stupid and hacky but async in C# makes completely no sense and it's the only way I could find to make this work
    }

    private void Pause(PauseState state)
    {
        CurrentPause = state;

        Server?.SendStopped(new()
        {
            ThreadId = 0,
            Reason = CurrentPause!.Value.HasBreakpoint ? StoppedEventReason.Breakpoint : StoppedEventReason.Step
        });
    }

    public async Task<AttachResponse> Handle(AttachRequestArguments request, CancellationToken cancellationToken)
    {
        Attached = true;
        return new();
    }

    public async Task<DisconnectResponse> Handle(DisconnectArguments request, CancellationToken cancellationToken)
    {
        SessionDone.TrySetResult(true);
        return new();
    }

    public async Task<SetBreakpointsResponse> Handle(SetBreakpointsArguments request, CancellationToken cancellationToken)
    {
        if (request.Breakpoints is not null)
        {
            ClearBreakpoints();

            foreach (var bp in request.Breakpoints)
            {
                AddBreakpoint(new(request.Source.Path ?? "", bp.Line, bp.Column ?? 0));
            }
        }

        return new();
    }

    public async Task<ThreadsResponse> Handle(ThreadsArguments request, CancellationToken cancellationToken)
    {
        return new()
        {
            Threads = new Container<OmniSharp.Extensions.DebugAdapter.Protocol.Models.Thread>([
                new OmniSharp.Extensions.DebugAdapter.Protocol.Models.Thread()
                {
                    Id = 0,
                    Name = "Main thread"
                }
            ])
        };
    }

    public async Task<StackTraceResponse> Handle(StackTraceArguments request, CancellationToken cancellationToken)
    {
        var loc = EnsureInterpreter.CurrentLocation?.Span.Start ?? throw new InvalidOperationException("No pause state found");

        return new()
        {
            StackFrames = new([
                new()
                {
                    Source = new()
                    {
                        Path = loc.FileName,
                    },
                    Line = loc.Line,
                    Column = loc.Column,
                }
            ])
        };
    }

    private const int LocalsReference = 1;
    private const int InputsReference = 2;

    public async Task<ScopesResponse> Handle(ScopesArguments request, CancellationToken cancellationToken)
    {
        return new()
        {
            Scopes = new([
                new()
                {
                    Name = "Locals",
                    VariablesReference = LocalsReference,
                    PresentationHint = "locals",
                },
                new()
                {
                    Name = "Inputs",
                    VariablesReference = InputsReference,
                    PresentationHint = "arguments",
                }
            ])
        };
    }

    public async Task<VariablesResponse> Handle(VariablesArguments request, CancellationToken cancellationToken)
    {
        return new()
        {
            Variables = request.VariablesReference switch
            {
                LocalsReference => new(EnsureInterpreter.GetAllLocals().Select(l => new Variable
                {
                    Name = l.Local.Name,
                    Value = FormatBitsValue(l.Value, l.Local.BitSize)
                })),
                InputsReference when EnsureInterpreter.Machine != null && EnsureInterpreter.Script != null
                    => new(EnsureInterpreter.Script.Inputs.Select(i => new Variable
                    {
                        Name = i.Key,
                        Value = FormatBitsValue(EnsureInterpreter.Machine.ReadInputs().Slice(i.Value.StartIndex, i.Value.BitSize), i.Value.BitSize)
                    })),
                _ => new()
            }
        };
    }

    public async Task<ContinueResponse> Handle(ContinueArguments request, CancellationToken cancellationToken)
    {
        Continue();

        return new();
    }

    public async Task<NextResponse> Handle(NextArguments request, CancellationToken cancellationToken)
    {
        Next();

        return new();
    }

    public async Task<StepInResponse> Handle(StepInArguments request, CancellationToken cancellationToken)
    {
        Next();

        return new();
    }

    public async Task<EvaluateResponse> Handle(EvaluateArguments request, CancellationToken cancellationToken)
    {
        var (value, errors) = EnsureInterpreter.Evaluate(request.Expression);

        return new()
        {
            Result = errors.Count > 0 ? $"Failed to parse: {string.Join(", ", [.. errors.Select(o => o.ToString())])}" : value.ToString()
        };
    }

    private static string FormatBitsValue(BitsValue value, int length) => $"{value.ToStringBinary(length)} ({value})";
}
