using System.Diagnostics.CodeAnalysis;
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

public class LogicScriptDebugger : IDebugger, IAttachHandler, IDisconnectHandler, ISetBreakpointsHandler, IThreadsHandler, IStackTraceHandler, IScopesHandler, IVariablesHandler, IContinueHandler, INextHandler, IEvaluateHandler, IStepInHandler, IPauseHandler
{
    private TaskCompletionSource<bool> SessionDone = new();

    private bool Attached;

    private readonly record struct PendingBreakpoint(int Number, SourceLocation Location, string? Condition);
    private readonly HashSet<PendingBreakpoint> PendingBreakpoints = [];

    private Interpreter EnsureInterpreter => CurrentPause?.Interpreter ?? throw new InvalidOperationException("Not currently paused");

    private DebugAdapterServer? Server;

    private LogicScriptDebugger()
    {
    }

    private async Task RunAsync(Stream input, Stream output)
    {
        SessionDone = new();

        ClearBreakpoints();

        using var server = DebugAdapterServer.Create(opts => opts
                    .WithInput(input)
                    .WithOutput(output)
                    .WithUnhandledExceptionHandler(_ => SessionDone.TrySetResult(false))
                    .AddHandler(this)
                );

        this.Server = server;

        await server.Initialize(CancellationToken.None);

        if (CurrentPause != null)
            Pause(CurrentPause.Value);

        await SessionDone.Task;

        Continue();
    }

    public static LogicScriptDebugger Launch(int port = 23475, bool singleClient = false) => Launch(new TcpListener(IPAddress.Loopback, port), singleClient);

    public static LogicScriptDebugger Launch(TcpListener listener, bool singleClient = false)
    {
        listener.Start();

        var debugger = new LogicScriptDebugger();

        _ = Task.Run(async () =>
        {
            try
            {
                do
                {
                    using var socket = await listener.AcceptSocketAsync();
                    using var stream = new NetworkStream(socket);

                    await debugger.RunAsync(stream, stream);
                } while (!singleClient);
            }
            finally
            {
                listener.Stop();
            }
        });

        return debugger;
    }

    public static LogicScriptDebugger Launch(Stream input, Stream output)
    {
        var debugger = new LogicScriptDebugger();

        _ = Task.Run(async () => await debugger.RunAsync(input, output));

        return debugger;
    }

    public static async Task<LogicScriptDebugger> LaunchAndWaitForAttachedAsync(int port = 23475)
    {
        var debugger = Launch(port);

        await debugger.WaitForAttachedAsync();

        return debugger;
    }

    public async Task WaitForAttachedAsync(CancellationToken cancellationToken = default)
    {
        while (!Attached)
            await Task.Delay(100, cancellationToken); // This is stupid and hacky but async in C# makes completely no sense and it's the only way I could find to make this work
    }

    private void Pause(PauseState state)
    {
        CurrentPause = state;

        Server?.SendStopped(new()
        {
            ThreadId = 0,
            Reason = state.HasBreakpoint ? StoppedEventReason.Breakpoint : StoppedEventReason.Step
        });
    }

    public void Stop()
    {
        Server?.SendTerminated(new());
    }

    private static string FormatBitsValue(BitsValue value, int length) => $"{value.ToStringBinary(length)} ({value})";

    #region Debugger

    private readonly record struct StatementBreakpoint(int Number, Statement Statement, string? Condition);

    private readonly record struct PauseState(int? BreakpointNumber, Statement Statement, Interpreter Interpreter)
    {
        public readonly TaskCompletionSource<bool> PauseBarrier = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool HasBreakpoint => BreakpointNumber != null;
    }

    private readonly Dictionary<int, StatementBreakpoint> Breakpoints = [];
    private readonly Mutex BreakpointsMutex = new();

    private readonly List<Script> LoadedScripts = [];
    private PauseState? CurrentPause;

    private int BreakpointCounter = 0;
    private bool IgnoreNext;
    private bool PauseNext;

    private Breakpoint AddBreakpoint(SourceLocation location, string? condition, int? wantNumber = null)
    {
        var verified = TryAddBreakpoint(location, condition, out var id, out var realLocation, wantNumber);
        if (!verified)
            return new Breakpoint
            {
                Id = id,
                Verified = false
            };

        return new Breakpoint
        {
            Id = id,
            Line = realLocation.Line,
            Column = realLocation.Column,
            Verified = true,
        };
    }

    private bool TryAddBreakpoint(SourceLocation location, string? condition, out int number, out SourceLocation realLocation, int? wantNumber = null)
    {
        BreakpointsMutex.WaitOne();
        number = wantNumber ?? BreakpointCounter++;

        try
        {
            if (TryFindStatement(location, out var stmt))
            {
                realLocation = stmt.Span.Start;

                Breakpoints.Add(number, new(number, stmt, condition));

                return true;
            }
            else
            {
                PendingBreakpoints.Add(new(number, location, condition));
            }
        }
        finally
        {
            BreakpointsMutex.ReleaseMutex();
        }

        realLocation = default;
        return false;
    }

    private bool TryFindStatement(SourceLocation location, [MaybeNullWhen(false)] out Statement stmt)
    {
        var script = LoadedScripts.FirstOrDefault(s => s.FileName == location.FileName);
        if (script != null)
        {
            foreach (var node in script.VisitAll())
            {
                if (node is Statement s && node.Span.Start.Line == location.Line && node.Span.Start.Column >= location.Column)
                {
                    stmt = s;
                    return true;
                }
            }
        }

        stmt = null;
        return false;
    }

    private void ClearBreakpoints(string? forFile = null)
    {
        BreakpointsMutex.WaitOne();

        if (forFile == null)
        {
            Breakpoints.Clear();
            PendingBreakpoints.Clear();
        }
        else
        {
            foreach (var key in Breakpoints.Keys.Where(k => Breakpoints[k].Statement.Span.Start.FileName == forFile).ToArray())
            {
                Breakpoints.Remove(key);
            }

            foreach (var pending in PendingBreakpoints.Where(p => p.Location.FileName == forFile).ToArray())
            {
                PendingBreakpoints.Remove(pending);
            }
        }

        BreakpointsMutex.ReleaseMutex();
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

        BreakpointsMutex.WaitOne();
        try
        {
            foreach (var bp in Breakpoints.Values)
            {
                if (bp.Statement == stmt)
                {
                    if (bp.Condition != null)
                    {
                        var (value, errors) = interpreter.Evaluate(bp.Condition);
                        if (errors.Count == 0 && value == 0)
                            continue;
                    }

                    Pause(new(bp.Number, stmt, interpreter));
                    pause = true;
                    break;
                }
            }
        }
        finally
        {
            BreakpointsMutex.ReleaseMutex();
        }
    }

    public async Task WaitForResumeAsync()
    {
        if (CurrentPause != null)
            await CurrentPause.Value.PauseBarrier.Task;
    }

    public void WaitForResume()
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        CurrentPause?.PauseBarrier.Task.Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
    }

    public void LoadedScript(Script script)
    {
        LoadedScripts.Add(script);

        foreach (var pending in PendingBreakpoints.ToArray())
        {
            var bp = AddBreakpoint(pending.Location, pending.Condition, pending.Number);

            if (bp.Verified)
            {
                Server?.SendBreakpoint(new()
                {
                    Breakpoint = bp,
                    Reason = BreakpointEventReason.Changed
                });

                PendingBreakpoints.Remove(pending);
            }
        }
    }

    #endregion

    #region Handlers

    public async Task<AttachResponse> Handle(AttachRequestArguments request, CancellationToken cancellationToken)
    {
        Attached = true;
        return new();
    }

    public async Task<DisconnectResponse> Handle(DisconnectArguments request, CancellationToken cancellationToken)
    {
        Attached = false;
        SessionDone.TrySetResult(true);

        return new();
    }

    public async Task<SetBreakpointsResponse> Handle(SetBreakpointsArguments request, CancellationToken cancellationToken)
    {
        if (request.Breakpoints is null)
            return new();

        var documentUri = "file://" + request.Source.Path;

        ClearBreakpoints(documentUri);

        return new()
        {
            Breakpoints = new(request.Breakpoints.Select(b => AddBreakpoint(new SourceLocation(documentUri, b.Line, b.Column ?? 0), b.Condition)))
        };
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
    private const int RegistersReference = 3;

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
                },
                new()
                {
                    Name = "Registers",
                    VariablesReference = RegistersReference,
                    PresentationHint = "registers",
                },
            ])
        };
    }

    public async Task<VariablesResponse> Handle(VariablesArguments request, CancellationToken cancellationToken)
    {
        return new()
        {
            Variables = request.VariablesReference switch
            {
                LocalsReference
                    => new(EnsureInterpreter.GetAllLocals().Select(l => new Variable
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
                RegistersReference when EnsureInterpreter.Machine != null && EnsureInterpreter.Script != null
                    => new(EnsureInterpreter.Script.Registers.Select(i => new Variable
                    {
                        Name = i.Key,
                        Value = FormatBitsValue(EnsureInterpreter.Machine.ReadRegister(i.Value.StartIndex), i.Value.BitSize)
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

    public async Task<PauseResponse> Handle(PauseArguments request, CancellationToken cancellationToken)
    {
        PauseNext = true;

        return new();
    }

    #endregion
}
