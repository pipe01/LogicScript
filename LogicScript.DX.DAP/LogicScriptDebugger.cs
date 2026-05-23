using System.Net;
using System.Net.Sockets;
using LogicScript.Interpreting;
using LogicScript.Interpreting.Debugging;
using OmniSharp.Extensions.DebugAdapter.Protocol.Events;
using OmniSharp.Extensions.DebugAdapter.Protocol.Models;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;
using OmniSharp.Extensions.DebugAdapter.Server;

namespace LogicScript.DX.DAP;

public class LogicScriptDebugger(Session session) : IAttachHandler, IDisconnectHandler, ISetBreakpointsHandler, IThreadsHandler, IStackTraceHandler, IScopesHandler, IVariablesHandler, IContinueHandler, INextHandler
{
    private TaskCompletionSource<bool> SessionDone = new();
    private readonly Session Session = session;

    private bool Attached;

    private Interpreter EnsureInterpreter => Session.CurrentPause?.Interpreter ?? throw new InvalidOperationException("Not currently paused");

    private DebugAdapterServer? Server;

    public async Task RunAsync(Stream input, Stream output)
    {
        SessionDone = new();

        Session.ClearBreakpoints();
        Session.Paused += OnSessionPaused;

        Server = DebugAdapterServer.Create(opts => opts
            .WithInput(input)
            .WithOutput(output)
            .WithUnhandledExceptionHandler(_ => SessionDone.TrySetResult(false))
            .AddHandler(this)
        );

        await Server.Initialize(CancellationToken.None);

        if (Session.CurrentPause != null)
            OnSessionPaused();

        await SessionDone.Task;

        Session.Paused -= OnSessionPaused;
    }

    public async Task RunSocketAsync(int port = 43532)
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

    public async Task WaitForAttachedAsync()
    {
        while (!Attached)
            await Task.Delay(100); // This is stupid and hacky but async in C# makes completely no sense and it's the only way I could find to make this work
    }

    private void OnSessionPaused()
    {
        Server?.SendStopped(new()
        {
            ThreadId = 0,
            Reason = Session.CurrentPause!.Value.HasBreakpoint ? StoppedEventReason.Breakpoint : StoppedEventReason.Step
        });
    }

    public async Task<AttachResponse> Handle(AttachRequestArguments request, CancellationToken cancellationToken)
    {
        Attached = true;

        Console.WriteLine("Attached");
        return new();
    }

    public async Task<DisconnectResponse> Handle(DisconnectArguments request, CancellationToken cancellationToken)
    {
        Console.WriteLine("Disconnected");
        SessionDone.TrySetResult(true);
        return new();
    }

    public async Task<SetBreakpointsResponse> Handle(SetBreakpointsArguments request, CancellationToken cancellationToken)
    {
        if (request.Breakpoints is not null)
        {
            foreach (var bp in request.Breakpoints)
            {
                Console.WriteLine($"Set breakpoint at {request.Source.Path}:{bp.Line}");

                Session.AddBreakpoint(new(request.Source.Path ?? "", bp.Line, bp.Column ?? 0));
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

    public async Task<ScopesResponse> Handle(ScopesArguments request, CancellationToken cancellationToken)
    {
        return new()
        {
            Scopes = new([
                new()
                {
                    Name = "Locals",
                    VariablesReference = 1,
                    PresentationHint = "locals",
                }
            ])
        };
    }

    public async Task<VariablesResponse> Handle(VariablesArguments request, CancellationToken cancellationToken)
    {
        return new()
        {
            Variables = new(EnsureInterpreter.GetAllLocals().Select(l => new Variable()
            {
                Name = l.Local.Name,
                Value = l.Value.ToString()
            }))
        };
    }

    public async Task<ContinueResponse> Handle(ContinueArguments request, CancellationToken cancellationToken)
    {
        Session.Continue();

        return new();
    }

    public async Task<NextResponse> Handle(NextArguments request, CancellationToken cancellationToken)
    {
        Session.Next();

        return new();
    }
}
