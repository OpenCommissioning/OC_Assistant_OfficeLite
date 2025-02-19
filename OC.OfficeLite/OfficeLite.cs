using System.Net.NetworkInformation;
using OC.Assistant.Sdk.Plugin;
using OC.Assistant.Sdk;
using OC.Assistant.Sdk.TcpIp;

namespace OC.OfficeLite;

[PluginIoType(IoType.Struct)]
public class OfficeLite : PluginBase
{
    [PluginParameter("IP Address of the\nOfficeLite VM")]
    private readonly string _iPAddress = "192.168.0.1";
        
    [PluginParameter("Default 50000")]
    private readonly int _port = 50000;
        
    [PluginParameter("Number of axes\nDefault 6\nUp to 12")]
    private readonly int _axisNo = 6;
        
    [PluginParameter("Size of the I/O image in bytes\nDefault 1024")]
    private readonly int _ioSize = 1024;
    
    [PluginParameter("Axis interpolation step time in milliseconds\n0 = no interpolation\nDefault value is 70")]
    private readonly int _interpolationTime = 70;

    private Client? _client;
    private byte[] _sendData = [];
    private byte[] _receiveData = [];
    private Interpolator[] _interpolator = [];
    private readonly StopwatchEx _stopwatch = new();

    protected override bool OnSave()
    {
        InputStructure.AddVariable("Input", TcType.Byte, _ioSize);
        OutputStructure.AddVariable("Output", TcType.Byte, _ioSize);
        OutputStructure.AddVariable("Axis", TcType.Real, _axisNo);
        return true;
    }

    protected override bool OnStart()
    {
        if (!CheckNetworkAdapters())
        {
            Logger.LogError(this, "No network available, please check network adapter or configuration");
            return false;
        }
            
        _stopwatch.Restart();
        _sendData = new byte [_ioSize];
        _receiveData = new byte[48 + _ioSize];
        _client = new Client(_iPAddress, _port);
        _client.Start();
        _client.OnConnected += ClientOnConnected;
        _client.OnServerMessage += ClientOnServerMessage;
        _client.OnError += message => Logger.LogWarning(this, message);
        _interpolator = new Interpolator[_axisNo];
        for (var i = 0; i < _axisNo; i++) _interpolator[i] = new Interpolator(_interpolationTime / 1000.0f);
        return true;
    }

    protected override void OnUpdate()
    {
        try
        {
            Array.Copy(InputBuffer, _sendData, _ioSize);

            if (_interpolationTime == 0)
            {
                Array.Copy(_receiveData, 0, OutputBuffer, 0, _ioSize + 4 * _axisNo);
                return;
            }
            
            Array.Copy(_receiveData, 0, OutputBuffer, 0, _ioSize);
            for (var i = 0; i < _axisNo; i++)
            {
                var offset = _ioSize + i * 4;
                var rawValue = BitConverter.ToSingle(_receiveData, offset);
                var interpolatedValue = BitConverter.GetBytes((float)_interpolator[i].Calculate(rawValue));
                Array.Copy(interpolatedValue, 0, OutputBuffer, offset, 4);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(this, e.Message, true);
            CancellationRequest();
        }
    }

    protected override void OnStop()
    {
        try
        {
            _client?.Stop();
        }
        catch (Exception e)
        {
            Logger.LogError(this, e.ToString());
        }
    }
        
    private static bool CheckNetworkAdapters()
    {
        var activeNetworks = from adapter in NetworkInterface.GetAllNetworkInterfaces()
            where adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet && adapter.OperationalStatus == OperationalStatus.Up
            select adapter;

        return activeNetworks.Any();
    }

    private void ClientOnServerMessage(byte[] buffer, int messageLength)
    {
        if (messageLength != _receiveData.Length) return;
        Array.Copy(buffer, 0, _receiveData, 0, messageLength);
        _stopwatch.WaitUntil(1);
        _client?.Send(_sendData);
    }

    private void ClientOnConnected()
    {
        Logger.LogInfo(this, $"Connected to {_iPAddress}:{_port}");
        _client?.Send(_sendData);
    }
}