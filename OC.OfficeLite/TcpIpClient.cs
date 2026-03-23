using System.Net.Sockets;

namespace OC.OfficeLite;

/// <summary>
/// Tcp/Ip Client.
/// </summary>
public class TcpIpClient : IDisposable
{
    private readonly byte[] _buffer;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly string _hostName;
    private readonly int _port;
    private readonly int _timeoutMilliseconds;
    private bool IsConnected => _client?.Connected ?? false;
    private bool _isConnecting;

    /// <summary>
    /// An error occured.
    /// </summary>
    public event Action<string>? OnError;
    
    /// <summary>
    /// The client received a message from the server.
    /// </summary>
    public event Action<byte[], int>? OnServerMessage;
    
    /// <summary>
    /// The client has been connected to the server.
    /// </summary>
    public event Action? OnConnected;
        
    /// <summary>
    /// Creates a new instance of the Tcp/Ip client.
    /// </summary>
    /// <param name="hostName">The server hostname.</param>
    /// <param name="port">The port the server is listening to.</param>
    /// <param name="timeoutMilliseconds">The timeout for receiving messages from the server.</param>
    /// <param name="bufferSize">The size of the buffer for receiving messages from the server.</param>
    public TcpIpClient(string hostName, int port, int timeoutMilliseconds = 5000, int bufferSize = 2048)
    {
        _hostName = hostName;
        _port = port;
        _timeoutMilliseconds = timeoutMilliseconds;
        _buffer = new byte[bufferSize];
    }

    /// <summary>
    /// Connects to the server.
    /// </summary>
    public void Start()
    {
        if (IsConnected || _isConnecting) return;
            
        _cancellationTokenSource = new CancellationTokenSource();
            
        var token = _cancellationTokenSource.Token;
            
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                if (!await WaitForConnection(token)) return;
                Receive(token);
            }
        }, token);
    }
        
    /// <summary>
    /// Closes the client and the stream.
    /// </summary>
    public void Stop()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _stream?.Close();
            _client?.Close();
        }
        catch (Exception e)
        {
            OnError?.Invoke(e.Message);
        }
    }

    private async Task<bool> WaitForConnection(CancellationToken token)
    {
        _isConnecting = true;
        while (!token.IsCancellationRequested && !IsConnected)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_hostName, _port, token);
            }
            catch (Exception e)
            {
                OnError?.Invoke(e.Message);
                Thread.Sleep(2000);
            }
        }

        if (_client is null)
        {
            return false;
        }
        _isConnecting = false;
        if (!IsConnected) return false;
        _client.ReceiveTimeout = _timeoutMilliseconds;
        _stream = _client.GetStream();
        OnConnected?.Invoke();
        return true;
    }

    /// <summary>
    /// Sends a message to the server.
    /// </summary>
    /// <param name="message">The message to be sent.</param>
    public void Send(byte[] message)
    {
        if (!IsConnected) return;

        try
        {
            _stream?.Write(message, 0, message.Length);
        }
        catch (Exception e)
        {
            OnError?.Invoke(e.Message);
        }
    }
        
    private void Receive(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!IsConnected || _stream is null) break;
                var length = _stream.Read(_buffer, 0, _buffer.Length);
                if (length == 0) break;
                OnServerMessage?.Invoke(_buffer, length);
            }
            catch(Exception e)
            {
                OnError?.Invoke(e.Message);
                break;
            }
        }

        try
        {
            _stream?.Close();
            _client?.Close();
        }
        catch(Exception e)
        {
            OnError?.Invoke(e.Message);
        }
    }

    public void Dispose()
    {
        Stop();
        _client?.Dispose();
        _stream?.Dispose();
        _cancellationTokenSource?.Dispose();
        GC.SuppressFinalize(this);
    }
}