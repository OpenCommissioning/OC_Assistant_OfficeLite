using System.Net;
using System.Net.Sockets;

namespace OC.OfficeLiteServer;

/// <summary>
/// Tcp/Ip Server.
/// </summary>
internal class TcpIpServer : TcpListener
{
    private CancellationTokenSource? _cancellationTokenSource;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private readonly byte[] _buffer;
    private readonly int _timeoutMilliseconds;
    private bool IsConnected => _client?.Connected ?? false;
    private bool _isConnecting;

    /// <summary>
    /// An error occured.
    /// </summary>
    public event Action<string>? OnError;
    
    /// <summary>
    /// The server received a message from the client.
    /// </summary>
    public event Action<byte[], int>? OnClientMessage;
    
    /// <summary>
    /// A new client has been connected to the server.
    /// </summary>
    public event Action<EndPoint>? OnConnected;
    
    /// <summary>
    /// The port the server is listening on.
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Creates a new instance of the Tcp/Ip server.
    /// </summary>
    /// <param name="ipAddress">IP address. Can also be <see cref="IPAddress.Any"/>.</param>
    /// <param name="port">The port the server is listening on.</param>
    /// <param name="timeoutMilliseconds">The timeout for receiving messages from the client.</param>
    /// <param name="bufferSize">The size of the buffer for receiving messages from the client.</param>
    public TcpIpServer(IPAddress ipAddress, int port, int timeoutMilliseconds = 5000, int bufferSize = 2048) : base(ipAddress, port)
    {
        Port = port;
        _timeoutMilliseconds = timeoutMilliseconds;
        _buffer = new byte[bufferSize];
    }

    /// <summary>
    /// Starts the server.
    /// </summary>
    public new void Start()
    {
        if (Active || _isConnecting) return;
            
        _cancellationTokenSource = new CancellationTokenSource();
        base.Start();
            
        var token = _cancellationTokenSource.Token;

        Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                if (!WaitForConnection(token)) continue;
                Receive(token);
            }
        }, token);
    }

    /// <summary>
    /// Stops the server and closes the stream.
    /// </summary>
    public new void Stop()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _stream?.Close();
            _client?.Close();
            base.Stop();
        }
        catch (Exception e)
        {
            OnError?.Invoke(e.Message);
        }
    }
        
    /// <summary>
    /// Sends a message.
    /// </summary>
    /// <param name="message">The message to be sent.</param>
    public void Send(byte[] message)
    {
        if (!IsConnected)
        {
            OnError?.Invoke("No client connected");
        }
            
        try
        {
            _stream?.Write(message, 0, message.Length);
        }
        catch(Exception e)
        {
            OnError?.Invoke(e.Message);
        }
    }

    private bool WaitForConnection(CancellationToken token)
    {
        _isConnecting = true;
        try
        {
            while (!token.IsCancellationRequested && !IsConnected)
            {
                if (!Pending())
                {
                    Thread.Sleep(500);
                    continue;
                }
                _client = AcceptTcpClient();
            }
        }
        catch (Exception e)
        {
            OnError?.Invoke(e.Message);
            _isConnecting = false;
            return false;
        }

        if (_client?.Client.RemoteEndPoint is null)
        {
            return false;
        }
        _isConnecting = false;
        if (!IsConnected) return false;
        _client.ReceiveTimeout = _timeoutMilliseconds;
        _stream = _client.GetStream();
        OnConnected?.Invoke(_client.Client.RemoteEndPoint);
        return true;
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
                OnClientMessage?.Invoke(_buffer, length);
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
}