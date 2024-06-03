using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Channels;
using Public.Frontend.Net.Utilities;
using Yarp.ReverseProxy.Forwarder;

namespace Public.Frontend.Net.Tunnel;

/// <summary>
/// The factory that YARP will use the create outbound connections by host name.
/// </summary>
public class TunnelClientFactory : ForwarderHttpClientFactory
{
    // TODO: These values should be populated by configuration so there's no need to remove
    // channels.
    private readonly ConcurrentDictionary<string, (Channel<int>, Channel<Stream>)> _clusterConnections = new();

    public (Channel<int>, Channel<Stream>) GetConnectionChannel(string key)
    {
        return _clusterConnections.GetOrAdd(key, _ => (Channel.CreateUnbounded<int>(), Channel.CreateUnbounded<Stream>()));
    }

    protected override void ConfigureHandler(ForwarderHttpClientContext context, SocketsHttpHandler handler)
    {
        base.ConfigureHandler(context, handler);

        var previous = handler.ConnectCallback ?? DefaultConnectCallback;

        static async ValueTask<Stream> DefaultConnectCallback(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
        {
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        handler.ConnectCallback = async (connectionContext, cancellationToken) =>
        {
            
            var connectionKey = connectionContext.GetConnectionKey();

            if (!string.IsNullOrEmpty(connectionKey) && _clusterConnections.TryGetValue(connectionKey, out var pair))
            {
                var (requests, responses) = pair;

                // Ask for a connection
                await requests.Writer.WriteAsync(0, cancellationToken);

                while (true)
                {
                    var stream = await responses.Reader.ReadAsync(cancellationToken);

                    if (stream is ICloseable c && c.IsClosed)
                    {
                        // Ask for another connection
                        await requests.Writer.WriteAsync(0, cancellationToken);

                        continue;
                    }

                    return stream;
                }
            }
            // return await previous(connectionContext, cancellationToken);
            // JC Don't try to access a site unless a backend connection is registered
            return Stream.Null;
        };
    }

    public List<string> GetConnectectClients()
    {
        return _clusterConnections.Select((n => n.Key)).ToList();
    }
}