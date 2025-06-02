using System.IO;
using System.Net;
using System.Xml.Linq;
using OC.Assistant.Sdk;

namespace OC.OfficeLiteServer;

public class Y200Server(Settings settings)
{
    /// <summary>
    /// Output[1024] + Axis[48] + Position[24] + Tool[24] + Base[24]
    /// </summary>
    private const uint DATA_SIZE_FROM_KRC = 1144;
    
    /// <summary>
    /// Input[1024]
    /// </summary>
    private const uint DATA_SIZE_TO_KRC = 1024;
    
    private readonly Assistant.Sdk.TcpIp.Server _tcpListener = new(IPAddress.Any, settings.Port);
    private readonly byte[] _sendBuffer = new byte[DATA_SIZE_FROM_KRC];
    private int _handle;
    
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
        if (!File.Exists(settings.Config))
        {
            Logger.LogError(this, $"'{settings.Config}' not found");
            return false;
        }
                               
        if (!File.Exists(Y200.PATH))
        {
            Logger.LogError(this, $"'{Y200.PATH}' not found");
            return false;
        }

        try
        {
            var config = XDocument.Load(settings.Config).Root;
            
            if (config?.Element("Y200IpcName")?.Value is not {} ipcName)
            {
                Logger.LogError(this, $"Error reading Y200WriteOffset from '{settings.Config}'");
                return false;
            }
            
            if (!uint.TryParse(config.Element("Y200WriteOffset")?.Value, out var writeOffset))
            {
                Logger.LogError(this, $"Error reading Y200WriteOffset from '{settings.Config}'");
                return false;
            }
            
            if (!uint.TryParse(config.Element("Y200ReadOffset")?.Value, out var readOffset))
            {
                Logger.LogError(this, $"Error reading Y200ReadOffset from '{settings.Config}'");
                return false;
            }
            
            var result = (Y200.Result)Y200.WMY200CreateIPCRange(
                ipcName,
                writeOffset,
                DATA_SIZE_FROM_KRC,
                readOffset,
                DATA_SIZE_TO_KRC,
                ref _handle);

            if (result == 0 && _handle != 0)
            {
                Logger.LogInfo(this, "Y200 connection ok");
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
            if (messageLength != DATA_SIZE_TO_KRC) return;
            
            var result = (Y200.Result)Y200.WMY200WriteBlock8(_handle, buffer, 0, DATA_SIZE_TO_KRC);
            if (result != Y200.Result.Ok)
            {
                Logger.LogError(this, $"WMY200WriteBlock8 error: {result}");
                return;
            }
            
            result = (Y200.Result)Y200.WMY200ReadBlock8(_handle, _sendBuffer, 0, DATA_SIZE_FROM_KRC);
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