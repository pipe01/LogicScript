using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LogicScript.DX.DAP;

namespace LogicScript.DX.LSP.Debugging
{
    public class DebugSession
    {
        public static DebugSession? Current { get; private set; }

        public LogicScriptDebugger Debugger { get; }
        private readonly TcpListener TcpListener;

        private readonly CancellationTokenSource CancellationTokenSource = new();
        public CancellationToken CancellationToken => CancellationTokenSource.Token;

        private DebugSession(LogicScriptDebugger debugger, TcpListener tcpListener)
        {
            this.Debugger = debugger;
            this.TcpListener = tcpListener;
        }

        public static IPEndPoint Start()
        {
            if (Current != null) throw new InvalidOperationException("Session already in progress");

            var listener = new TcpListener(IPAddress.Loopback, 0);
            var debugger = LogicScriptDebugger.Launch(listener, true);

            Current = new(debugger, listener);

            return (IPEndPoint)listener.LocalEndpoint;
        }

        public void Stop()
        {
            Debugger.Stop();
            TcpListener.Stop();
            CancellationTokenSource.Cancel();

            if (Current == this)
                Current = null;
        }
    }
}