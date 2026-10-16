using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using OC.Assistant.Sdk;

namespace OC.OfficeLite;

/// <summary>
/// Client for the Y200 Remote Server (Y200_Svr.exe).<br/>
/// Cyclically writes a range into and reads a range from the Y200 shared memory process image.
/// </summary>
/// <remarks>
/// Every telegram starts with a 12 byte little endian header:
/// CRC-16/ARC over bytes [2..length), telegram type, 4 bytes echoed by the server (tag and command), total length.
/// The type selects the command in requests and the kind of reply in replies.
/// </remarks>
public class Y200Client : IDisposable
{
    private const int HEADER_SIZE = 12;
    private const int MAX_TELEGRAM_SIZE = 0x8010;
    private const uint MUTEX_TIMEOUT_MILLISECONDS = 1000;
    private const string CLIENT_ID = "M&P WinMOD Y200 Remote Client MWF\0";
    private const string DEFAULT_IPC_NAME = "WM3_MEM_Y200";

    private const ushort CMD_HELLO = 0;
    private const ushort CMD_WRITE = 4;
    private const ushort CMD_READ = 5;
    private const ushort CMD_DISCONNECT = 6;
    private const ushort CMD_OPEN_IMAGE = 7;

    private const ushort KIND_HELLO = 0;
    private const ushort KIND_ACK = 1;
    private const ushort KIND_TEXT = 2;
    private const ushort KIND_READ = 4;
    private const ushort KIND_IMAGE = 8;

    private static readonly ushort[] CrcTable = CreateCrcTable();

    private readonly string _hostName;
    private readonly int _port;
    private readonly string _ipcName;
    private readonly uint _readOffset;
    private readonly uint _writeOffset;
    private readonly int _timeoutMilliseconds;
    private readonly byte[] _readData;
    private readonly byte[] _writeData;
    private readonly byte[] _writeSnapshot;
    private readonly byte[] _receiveBuffer = new byte[MAX_TELEGRAM_SIZE];
    private readonly StopwatchEx _stopwatch = new();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _task;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private ushort _tag;
    private byte[] _lastTelegram = [];

    /// <summary>
    /// An error occured.
    /// </summary>
    public event Action<string>? OnError;

    /// <summary>
    /// The client has been connected to the server and the process image is open.
    /// </summary>
    public event Action? OnConnected;

    /// <summary>
    /// New data has been read from the process image.
    /// </summary>
    public event Action<byte[]>? OnReadData;

    /// <summary>
    /// Creates a new instance of the Y200 client.
    /// </summary>
    /// <param name="hostName">The server hostname.</param>
    /// <param name="port">The port Y200_Svr.exe is listening on.</param>
    /// <param name="ipcName">The name of the shared memory process image. Empty selects WM3_MEM_Y200.</param>
    /// <param name="readOffset">Offset of the range to be read cyclically.</param>
    /// <param name="readSize">Size of the range to be read cyclically.</param>
    /// <param name="writeOffset">Offset of the range to be written cyclically.</param>
    /// <param name="writeSize">Size of the range to be written cyclically.</param>
    /// <param name="timeoutMilliseconds">The timeout for receiving messages from the server.</param>
    public Y200Client(string hostName, int port, string ipcName, uint readOffset, uint readSize,
        uint writeOffset, uint writeSize, int timeoutMilliseconds = 5000)
    {
        _hostName = hostName;
        _port = port;
        //The server rejects command 7 without a name, so the default has to be sent explicitly
        _ipcName = string.IsNullOrWhiteSpace(ipcName) ? DEFAULT_IPC_NAME : ipcName.Trim();
        _readOffset = readOffset;
        _writeOffset = writeOffset;
        _timeoutMilliseconds = timeoutMilliseconds;
        _readData = new byte[readSize];
        _writeData = new byte[writeSize];
        _writeSnapshot = new byte[writeSize];
    }

    /// <summary>
    /// Connects to the server and starts the cyclic data exchange.
    /// </summary>
    public void Start()
    {
        if (_task is not null) return;
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;
        _task = Task.Run(() => Run(token), token);
    }

    /// <summary>
    /// Stops the data exchange and closes the connection.
    /// </summary>
    public void Stop()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            if (_task?.Wait(_timeoutMilliseconds) == false)
            {
                _stream?.Close();
                _client?.Close();
            }
        }
        catch (Exception e)
        {
            OnError?.Invoke(e.Message);
        }
        _task = null;
    }

    /// <summary>
    /// Sets the data to be written to the process image with the next cycle.
    /// </summary>
    /// <param name="data">The source buffer.</param>
    /// <param name="offset">The offset in the source buffer.</param>
    public void SetWriteData(byte[] data, int offset = 0)
    {
        lock (_writeData)
        {
            Array.Copy(data, offset, _writeData, 0, _writeData.Length);
        }
    }

    private void Run(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                Connect(token);
                Hello();
                OpenImage();
                OnConnected?.Invoke();
                _stopwatch.Restart();

                while (!token.IsCancellationRequested)
                {
                    Exchange();
                    _stopwatch.WaitUntil(1);
                }

                Disconnect();
            }
            catch (Exception e)
            {
                if (token.IsCancellationRequested) break;
                OnError?.Invoke(e.Message);
                token.WaitHandle.WaitOne(2000);
            }
            finally
            {
                _stream?.Close();
                _client?.Close();
            }
        }
    }

    private void Connect(CancellationToken token)
    {
        _client = new TcpClient { NoDelay = true };
        _client.ConnectAsync(_hostName, _port, token).AsTask().GetAwaiter().GetResult();
        _client.ReceiveTimeout = _timeoutMilliseconds;
        _client.SendTimeout = _timeoutMilliseconds;
        _stream = _client.GetStream();
    }

    private void Hello()
    {
        var id = Encoding.ASCII.GetBytes(CLIENT_ID);
        var payload = new byte[8 + id.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(0), 1);
        id.CopyTo(payload, 8);
        Request(CMD_HELLO, payload, KIND_HELLO);
    }

    private void OpenImage()
    {
        var size = Math.Max(_readOffset + (uint)_readData.Length, _writeOffset + (uint)_writeData.Length);
        var name = Encoding.ASCII.GetBytes(_ipcName);
        var payload = new byte[20 + name.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(0), MUTEX_TIMEOUT_MILLISECONDS);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4), (uint)_timeoutMilliseconds);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(8), size);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(12), size);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(16), size);
        name.CopyTo(payload, 20);

        var reply = Request(CMD_OPEN_IMAGE, payload, KIND_IMAGE);
        var actualSize = BinaryPrimitives.ReadUInt32LittleEndian(reply.AsSpan(4));
        if (actualSize < size)
        {
            throw new InvalidOperationException(
                $"Process image '{_ipcName}' has {actualSize} bytes, but {size} bytes are required");
        }
    }

    private void Exchange()
    {
        lock (_writeData)
        {
            Array.Copy(_writeData, _writeSnapshot, _writeData.Length);
        }

        var writePayload = new byte[4 + _writeSnapshot.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(writePayload.AsSpan(0), _writeOffset);
        _writeSnapshot.CopyTo(writePayload, 4);

        var readPayload = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(readPayload.AsSpan(0), _readOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(readPayload.AsSpan(4), (uint)_readData.Length);

        //Both requests are sent at once, the server answers them in order
        var writeTag = Send(CMD_WRITE, writePayload);
        var readTag = Send(CMD_READ, readPayload);
        Receive(writeTag, CMD_WRITE, KIND_ACK);
        var reply = Receive(readTag, CMD_READ, KIND_READ);
        if (reply is null) return;

        if (reply.Length < 4 + _readData.Length)
        {
            throw new InvalidDataException($"Read reply too short ({reply.Length} bytes)");
        }

        Array.Copy(reply, 4, _readData, 0, _readData.Length);
        OnReadData?.Invoke(_readData);
    }

    private void Disconnect()
    {
        try
        {
            Request(CMD_DISCONNECT, [], KIND_ACK);
        }
        catch
        {
            //Connection is closed anyway
        }
    }

    private byte[] Request(ushort command, byte[] payload, ushort kind)
    {
        var tag = Send(command, payload);
        return Receive(tag, command, kind) ??
               throw new InvalidOperationException(
                   $"Command {command} has been rejected by the server. Sent telegram: {Convert.ToHexString(_lastTelegram)}");
    }

    private ushort Send(ushort command, byte[] payload)
    {
        var length = HEADER_SIZE + payload.Length;
        if (length > MAX_TELEGRAM_SIZE) throw new ArgumentException("Telegram too long");

        _tag++;
        var telegram = new byte[length];
        BinaryPrimitives.WriteUInt16LittleEndian(telegram.AsSpan(2), command);
        BinaryPrimitives.WriteUInt16LittleEndian(telegram.AsSpan(4), _tag);
        BinaryPrimitives.WriteUInt16LittleEndian(telegram.AsSpan(6), command);
        BinaryPrimitives.WriteUInt32LittleEndian(telegram.AsSpan(8), (uint)length);
        payload.CopyTo(telegram, HEADER_SIZE);
        BinaryPrimitives.WriteUInt16LittleEndian(telegram.AsSpan(0), Crc16(telegram.AsSpan(2)));

        _stream!.Write(telegram, 0, telegram.Length);
        _lastTelegram = telegram;
        return _tag;
    }

    /// <summary>
    /// Receives the reply to a request.
    /// Returns the payload, or null if the server answered with a text message instead.
    /// </summary>
    private byte[]? Receive(ushort tag, ushort command, ushort kind)
    {
        _stream!.ReadExactly(_receiveBuffer, 0, HEADER_SIZE);
        var length = (int)BinaryPrimitives.ReadUInt32LittleEndian(_receiveBuffer.AsSpan(8));
        if (length is < HEADER_SIZE or > MAX_TELEGRAM_SIZE)
        {
            throw new InvalidDataException($"Invalid telegram length {length}");
        }

        _stream.ReadExactly(_receiveBuffer, HEADER_SIZE, length - HEADER_SIZE);
        if (Crc16(_receiveBuffer.AsSpan(2, length - 2)) != BinaryPrimitives.ReadUInt16LittleEndian(_receiveBuffer))
        {
            throw new InvalidDataException("Telegram CRC mismatch");
        }

        var replyKind = BinaryPrimitives.ReadUInt16LittleEndian(_receiveBuffer.AsSpan(2));
        var replyTag = BinaryPrimitives.ReadUInt16LittleEndian(_receiveBuffer.AsSpan(4));
        var replyCommand = BinaryPrimitives.ReadUInt16LittleEndian(_receiveBuffer.AsSpan(6));
        var payload = _receiveBuffer.AsSpan(HEADER_SIZE, length - HEADER_SIZE);

        if (replyKind == KIND_TEXT)
        {
            var text = payload.Length > 4 ? Encoding.Latin1.GetString(payload[4..]).TrimEnd('\0') : "";
            OnError?.Invoke($"Y200 server: {text}");
            return null;
        }

        if (replyKind != kind || replyTag != tag || replyCommand != command)
        {
            throw new InvalidDataException(
                $"Unexpected reply (kind {replyKind}, command {replyCommand}) to command {command}");
        }

        return payload.ToArray();
    }

    private static ushort Crc16(ReadOnlySpan<byte> data)
    {
        ushort crc = 0;
        foreach (var b in data)
        {
            crc = (ushort)(CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8));
        }
        return crc;
    }

    private static ushort[] CreateCrcTable()
    {
        var table = new ushort[256];
        for (var i = 0; i < 256; i++)
        {
            var crc = (ushort)i;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
            }
            table[i] = crc;
        }
        return table;
    }

    public void Dispose()
    {
        Stop();
        _client?.Dispose();
        _stream?.Dispose();
        _cancellationTokenSource?.Dispose();
        _stopwatch.Dispose();
        GC.SuppressFinalize(this);
    }
}
