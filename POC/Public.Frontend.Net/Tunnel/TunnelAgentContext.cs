using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Public.Frontend.Net.Utilities;

namespace Public.Frontend.Net.Tunnel
{
    internal class TunnelAgentContext
    {
        private HttpContext _connectionContext;
        private TunnelClientFactory _tunnelClientFactory;
        private readonly IHostApplicationLifetime _lifetime;
        public TunnelAgentContext()
        {
            //_connectionContext = connectionContext;
            //_tunnelClientFactory = new TunnelClientFactory();
        }


        public async Task<IResult> MapConnectionInit(HttpContext context, IHostApplicationLifetime lifetime)
        {
            if (context.Request.Protocol != HttpProtocol.Http2)
            {
                return Results.BadRequest();
            }

            var connectionKey = context.GetConnectionKey();

            StaticLogger.Logger.LogInformation(StaticLogger.GetWrappedMessage($"{connectionKey} connected"));

            _tunnelClientFactory = new TunnelClientFactory();
            var (requests, responses) = _tunnelClientFactory.GetConnectionChannel(connectionKey);

            await requests.Reader.ReadAsync(context.RequestAborted);

            var stream = new DuplexHttpStream(context);

            using var reg = lifetime.ApplicationStopping.Register(() => stream.Abort());

            // Keep reusing this connection while, it's still open on the backend
            while (!context.RequestAborted.IsCancellationRequested)
            {
                // Make this connection available for requests
                await responses.Writer.WriteAsync(stream, context.RequestAborted);

                await stream.StreamCompleteTask;

                stream.Reset();
            }

            return Results.Empty;
        }
    }
}
