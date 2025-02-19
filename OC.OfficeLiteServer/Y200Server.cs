using System.IO;
using System.Net;
using System.Xml.Linq;
using OC.Assistant.Sdk;

namespace OC.OfficeLiteServer;

public class Y200Server
{
    private readonly Assistant.Sdk.TcpIp.Server _tcpListener;
    private readonly Settings _settings;
    private readonly byte[] _sendBuffer;
    private int _handle;

    public Y200Server(Settings settings)
    {
        _settings = settings;
        _sendBuffer = new byte[48 + _settings.IoSize];
        _tcpListener = new Assistant.Sdk.TcpIp.Server(IPAddress.Any, settings.Port);
    }
        
    public void Start()
    {
        if (_handle != 0) return; 
            
        Task.Run(() =>
        {
            if (!InitializeY200()) return;
            StartTcpServer();
        });
    }

    public void Stop()
    {
        if (_handle == 0) return;
        _tcpListener.Stop();
        var result = (Y200.Result)Y200.WMY200DestroyIPCRange(_handle);
        if (result != Y200.Result.Ok)
        {
            Logger.LogError(this, $"WMY200DestroyIPCRange error: {result}");
        }
    }

    private bool InitializeY200()
    {
        if (!File.Exists(_settings.Config))
        {
            Logger.LogError(this, $"'{_settings.Config}' not found");
            return false;
        }
                               
        if (!File.Exists(Y200.PATH))
        {
            Logger.LogError(this, $"'{Y200.PATH}' not found");
            return false;
        }

        try
        {
            var config = XDocument.Load(_settings.Config).Root;
            
            if (config?.Element("Y200IpcName")?.Value is not {} ipcName)
            {
                Logger.LogError(this, $"Error reading Y200WriteOffset from '{_settings.Config}'");
                return false;
            }
            
            if (!uint.TryParse(config.Element("Y200WriteOffset")?.Value, out var writeOffset))
            {
                Logger.LogError(this, $"Error reading Y200WriteOffset from '{_settings.Config}'");
                return false;
            }
            
            if (!uint.TryParse(config.Element("Y200ReadOffset")?.Value, out var readOffset))
            {
                Logger.LogError(this, $"Error reading Y200ReadOffset from '{_settings.Config}'");
                return false;
            }
            
            var result = (Y200.Result)Y200.WMY200CreateIPCRange(
                ipcName,
                writeOffset,
                1160u,
                readOffset,
                1040u,
                ref _handle);

            if (result == 0 && _handle != 0)
            {
                Logger.LogInfo(this, $"Y200 connection ok, I/O size {_settings.IoSize}");
                return true;
            }
            
            Logger.LogError(this, $"WMY200CreateIPCRange error: {result}");
            return false;
        }
        catch (Exception e)
        {
            Logger.LogError(this, e.Message);
            return false;
        }
    }
        
    private void StartTcpServer()
    {
        try
        {
            _tcpListener.OnClientMessage += TcpListenerOnClientMessage;
            _tcpListener.OnError += message => Logger.LogError(this, message);
            _tcpListener.OnConnected += endPoint => Logger.LogInfo(this, $"Connected to {endPoint}");
            _tcpListener.Start();
            Logger.LogInfo(this, $"Listening on port {_tcpListener.Port}");
        }
        catch (Exception e)
        {
            Logger.LogError(this, e.Message);
        }
    }

    private void TcpListenerOnClientMessage(byte[] buffer, int messageLength)
    {
        try
        {
            if (messageLength != _settings.IoSize) return;
            var result = (Y200.Result)Y200.WMY200WriteBlock8(_handle, buffer, 0, (uint)_settings.IoSize);
            if (result != Y200.Result.Ok)
            {
                Logger.LogError(this, $"WMY200WriteBlock8 error: {result}");
                return;
            }
            result = (Y200.Result)Y200.WMY200ReadBlock8(_handle, _sendBuffer, 0, 48 + (uint)_settings.IoSize);
            if (result != Y200.Result.Ok)
            {
                Logger.LogError(this, $"WMY200ReadBlock8 error: {result}");
                return;
            }
            _tcpListener.Send(_sendBuffer);
        }
        catch(Exception e)
        {
            Logger.LogWarning(this, e.Message);
        }
    }
}