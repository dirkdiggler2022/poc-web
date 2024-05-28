using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Private.Agent.Net.Monitoring
{
    public class ConnectionMonitor
    {
        private readonly IConnectionListenerFactory _connectionListenerFactory;

        public ConnectionMonitor(IConnectionListenerFactory connectionListenerFactory)
        {
            _connectionListenerFactory = connectionListenerFactory;
        }


        public void Start()
        {

            Timer timer = new Timer(RunTask, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        void RunTask(object state)
        {
            Console.WriteLine($"Task running at {DateTime.Now}");
            var tc = _connectionListenerFactory as TunnelConnectionListenerFactory;
            if (tc is null or { TunnelConnectionListener: null })
                return;
            var connections =
                tc.TunnelConnectionListener.Connections.Any(n => n.Value.ConnectionClosed.IsCancellationRequested);
        }
    
    }
}
