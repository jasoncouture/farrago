namespace Farrago.Protocol.Tcp.Server;

public class NativeTcpServerOptions
{
    public bool Enabled { get; set; } = false;
    public int Port { get; set; } = 9736;
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<string> Addresses { get; } = new();
}