using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using LogicScript.Compiling;
using LogicScript.Data;
using LogicScript.Interpreting;
using LogicScript.Parsing;
using LogicScript.Parsing.Structures;
using LogicScript.Parsing.Structures.Statements;
using OmniSharp.Extensions.DebugAdapter.Protocol.Events;
using OmniSharp.Extensions.DebugAdapter.Protocol.Models;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;
using OmniSharp.Extensions.DebugAdapter.Server;

namespace LogicScript.DX.DAP;

public class LogicScriptDebugger : IDebugger, IAttachHandler, IDisconnectHandler, ISetBreakpointsHandler, IThreadsHandler, IStackTraceHandler, IScopesHandler, IVariablesHandler, IContinueHandler, INextHandler, IStepInHandler, IPauseHandler, IEvaluateHandler
{
    private TaskCompletionSource<bool> SessionDone = new();

    private bool Attached;

    private readonly record struct PendingBreakpoint(int Number, SourceLocation Location);
    private readonly HashSet<PendingBreakpoint> PendingBreakpoints = [];

    private record Frame();

    private readonly Stack<Frame> StackFrames = new();

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
            Pause(CurrentPause);

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
        Debug.WriteLine($"  Current statement is: ({state.Statement.ID}) {state.Statement.Span}");

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

    private bool TryFindNode<T>(NodeID id, [MaybeNullWhen(false)] out T node, [MaybeNullWhen(false)] out Script script) where T : IIdentifiableCodeNode
    {
        foreach (var sc in LoadedScripts)
        {
            foreach (var n in sc.VisitAll().OfType<T>())
            {
                if (n.ID == id)
                {
                    node = n;
                    script = sc;
                    return true;
                }
            }
        }

        node = default;
        script = null;
        return false;
    }

    #region Debugger

    private readonly record struct StatementBreakpoint(int Number, Statement Statement);

    private record class PauseState(int? BreakpointNumber, Statement Statement, IScriptInstance CompiledScript, IMachine Machine, Script Script)
    {
        public readonly TaskCompletionSource<bool> PauseBarrier = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool HasBreakpoint => BreakpointNumber != null;
    }

    private readonly Dictionary<int, StatementBreakpoint> Breakpoints = [];
    private readonly Mutex BreakpointsMutex = new();

    private readonly List<Script> LoadedScripts = [];
    private PauseState? CurrentPause;

    private readonly Dictionary<NodeID, ulong> CurrentLocals = [];

    private int BreakpointCounter = 0;
    private bool PauseNext;

    private Breakpoint AddBreakpoint(SourceLocation location, int? wantNumber = null)
    {
        var verified = TryAddBreakpoint(location, out var id, out var realLocation, wantNumber);
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

    private bool TryAddBreakpoint(SourceLocation location, out int number, out SourceLocation realLocation, int? wantNumber = null)
    {
        BreakpointsMutex.WaitOne();
        number = wantNumber ?? BreakpointCounter++;

        try
        {
            if (TryFindStatement(location, out var stmt))
            {
                realLocation = stmt.Span.Start;

                Breakpoints.Add(number, new(number, stmt));

                return true;
            }
            else
            {
                PendingBreakpoints.Add(new(number, location));
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
        CurrentPause?.PauseBarrier.TrySetResult(true);
    }

    public void Next()
    {
        if (CurrentPause != null)
        {
            PauseNext = true;
            Continue();
        }
    }

    void IDebugger.PushLocal(NodeID id)
    {
        CurrentLocals.Add(id, 0);
    }

    void IDebugger.SetLocal(NodeID id, ulong value)
    {
        CurrentLocals[id] = value;
    }

    void IDebugger.PopLocal(NodeID id)
    {
        CurrentLocals.Remove(id);
    }

    void IDebugger.TraceStatement(IScriptInstance compiledScript, IMachine machine, NodeID id)
    {
        if (!Attached || !TryFindNode<Statement>(id, out var stmt, out var script) || stmt is BlockStatement)
            return;

        if (PauseNext)
        {
            Debug.WriteLine("Pausing due to PauseNext == true");

            PauseNext = false;

            Pause(new(null, stmt, compiledScript, machine, script));
            WaitForResume();
            return;
        }

        BreakpointsMutex.WaitOne();
        try
        {
            foreach (var bp in Breakpoints.Values)
            {
                if (bp.Statement.ID == id)
                {
                    Debug.WriteLine("Pausing due to hit breakpoint");

                    Pause(new(bp.Number, bp.Statement, compiledScript, machine, script));
                    WaitForResume();
                    break;
                }
            }
        }
        finally
        {
            BreakpointsMutex.ReleaseMutex();
        }
    }

    void IDebugger.GotOutput(string line)
    {
        Server?.SendOutput(new()
        {
            Output = line,
        });
    }

    void IDebugger.PushFunctionCall(NodeID functionId)
    {
        // TODO: implement
    }

    void IDebugger.PopFunctionCall()
    {
        // TODO: implement
    }

    public async Task WaitForResumeAsync()
    {
        if (CurrentPause != null)
            await CurrentPause.PauseBarrier.Task;
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
            var bp = AddBreakpoint(pending.Location, pending.Number);

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
            Breakpoints = new(request.Breakpoints.Select(b => AddBreakpoint(new SourceLocation(documentUri, b.Line, b.Column ?? 0))))
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
        var span = CurrentPause!.Statement.Span;

        return new()
        {
            StackFrames = new([
                new()
                {
                    Source = new()
                    {
                        Path = span.Start.FileName,
                    },
                    Line = span.Start.Line,
                    Column = span.Start.Column,
                    EndLine = span.End.Line,
                    EndColumn = span.End.Column,
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
                    => new(
                        CurrentLocals
                        .Select(l =>
                        {
                            if (!TryFindNode<LocalInfo>(l.Key, out var localInfo, out _))
                                return new();

                            return new Variable
                            {
                                Name = localInfo.Name,
                                Value = FormatBitsValue(l.Value, localInfo.BitSize)
                            };
                        })
                    ),
                InputsReference => new(CurrentPause!.Script.Inputs.Select(p => PortVariable(p.Key, p.Value))),
                RegistersReference => new(CurrentPause!.Script.Registers.Select(p => PortVariable(p.Key, p.Value))),
                _ => new(PortVectorVariables(request.VariablesReference, (int)(request.Start ?? 0), (int)(request.Count ?? int.MaxValue)))
            }
        };

        Variable PortVariable(string name, MachinePortInfo port)
        {
            if (port.VectorLength != 1)
            {
                return new Variable
                {
                    Name = name,
                    Value = $"[{port.VectorLength}]",
                    VariablesReference = Math.Abs((long)port.GetHashCode()),
                    IndexedVariables = port.VectorLength,
                };
            }

            return new Variable
            {
                Name = name,
                Value = FormatBitsValue(port.Target switch
                {
                    MachinePorts.Input => CurrentPause!.Machine.ReadInputs(port.StartIndex, port.BitSize),
                    MachinePorts.Register => CurrentPause!.CompiledScript.Registers.GetRegister(port.StartIndex, 0),
                    _ => throw new NotImplementedException()
                }, port.BitSize)
            };
        }

        IEnumerable<Variable> PortVectorVariables(long reference, int start, int count)
        {
            MachinePortInfo port =
                CurrentPause!.Script.Inputs.Values
                .Concat(CurrentPause!.Script.Registers.Values)
                .FirstOrDefault(p => Math.Abs((long)p.GetHashCode()) == reference);

            if (port.Target == MachinePorts.Placeholder)
                return [];

            return Enumerable.Range(start, Math.Min(count, port.VectorLength))
                .Select(vi => new Variable()
                {
                    Name = $"[{vi}]",
                    Value = FormatBitsValue(port.Target switch
                    {
                        MachinePorts.Input => CurrentPause!.Machine.ReadInputs(port.StartIndex + vi * port.BitSize, port.BitSize),
                        MachinePorts.Register => CurrentPause!.CompiledScript.Registers.GetRegister(port.StartIndex, vi),
                        _ => throw new NotImplementedException()
                    }, port.BitSize)
                });
        }
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

    public async Task<PauseResponse> Handle(PauseArguments request, CancellationToken cancellationToken)
    {
        PauseNext = true;

        return new();
    }

    public async Task<EvaluateResponse> Handle(EvaluateArguments request, CancellationToken cancellationToken)
    {
        if (CurrentPause == null)
            throw new InvalidOperationException("Can't evaluate expression while program is running");

        var locals = CurrentLocals.Select(o => (CurrentPause.Script.VisitAll().OfType<LocalInfo>().First(f => f.ID == o.Key), o.Value));

        var (parsed, errors) = CurrentPause.Script.ParseExpression(request.Expression, [.. locals.Select(p => p.Item1)]);
        if (parsed == null)
        {
            return new()
            {
                Result = $"Failed to parse: {string.Join(", ", [.. errors.Select(o => o.ToString())])}"
            };
        }

        var result = Interpreter.Visit(parsed, new(CurrentPause.Machine, CurrentPause.CompiledScript.Registers, locals.ToDictionary(p => p.Item1, p => p.Value)));

        return new()
        {
            Result = result.ToString()
        };
    }

    #endregion
}
