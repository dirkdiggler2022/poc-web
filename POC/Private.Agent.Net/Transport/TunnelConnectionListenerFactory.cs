using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;

public class TunnelConnectionListenerFactory : IConnectionListenerFactory
{
    private readonly TunnelOptions _options;
    private TunnelConnectionListener _tunnelConnectionListener;

    
    public TunnelConnectionListener TunnelConnectionListener
    {
        get { return _tunnelConnectionListener; }
    }

    public TunnelConnectionListenerFactory(IOptions<TunnelOptions> 
    options)
    {
        _options = options.Value;
    }

    public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        _tunnelConnectionListener = new TunnelConnectionListener(_options, endpoint);
        return new(_tunnelConnectionListener);
    }
}