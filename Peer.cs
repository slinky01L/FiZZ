using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FiZZ;

public class PeerListener(ushort port)
{
    private TcpListener? _listener;
    
    public bool Start(out string err)
    {
        err = string.Empty;
        
        try
        {
            _listener = new TcpListener(IPAddress.Any, port);
        }
        catch (Exception e)
        {
            err = e.Message;
            return false;
        }

        return true;
    }
}

public class PeerConnectionState
{
    public bool AmChoking = true;
    public bool AmInterested = false;
    public bool IsChoking = true;
    public bool IsInterested = false;
}

public class PeerMessage
{

}

public class PeerConnection(Action<byte[]> onMessageReceived)
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private readonly Action<byte[]>? _onMessageReceived = onMessageReceived;

    public async Task ConnectAsync(string host, int port)
    {
        if (_client != null && _client.Connected)
            throw new InvalidOperationException("Client is already connected.");

        _client = new TcpClient();
        _cts = new CancellationTokenSource();

        await _client.ConnectAsync(host, port);
        _stream = _client.GetStream();
        
        _ = StartListeningLoopAsync(_cts.Token);
    }
    
    public async Task SendMessageAsync(byte[] data)
    {
        if (_stream == null || ((!_client?.Connected) ?? false))
            throw new InvalidOperationException("Client is not connected.");
        
        await _stream.WriteAsync(data, _cts?.Token ?? CancellationToken.None);
    }

    private async Task StartListeningLoopAsync(CancellationToken token)
    {
        var buffer = new byte[4096];

        try
        {
            while (!token.IsCancellationRequested && ((!_client?.Connected) ?? false))
            {
                var bytesRead = await _stream!.ReadAsync(buffer, token);
                
                if (bytesRead == 0)
                {
                    Console.WriteLine("Server closed connection.");
                    break;
                }
                
                _onMessageReceived?.Invoke(buffer[..bytesRead]);
            }
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in listening loop: {ex.Message}");
        }
        finally
        {
            Disconnect();
        }
    }
    
    public void Disconnect()
    {
        _cts?.Cancel();
        _stream?.Dispose();
        _client?.Dispose();
        
        _stream = null;
        _client = null;
        _cts = null;
    }

    public void Dispose()
    {
        Disconnect();
    }
}