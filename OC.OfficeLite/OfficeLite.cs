using System.Net.NetworkInformation;
using OC.Assistant.Sdk.Plugin;
using OC.Assistant.Sdk;
using OC.Assistant.Sdk.TcpIp;

namespace OC.OfficeLite;

[PluginIoType(IoType.Struct)]
public class OfficeLite : PluginBase
{
    /// <summary>
    /// Output[1024] + Axis[48] + Position[24] + Tool[24] + Base[24]
    /// </summary>
    private const uint DATA_SIZE_FROM_KRC = 1144;
    
    /// <summary>
    /// Input[1024]
    /// </summary>
    private const uint DATA_SIZE_TO_KRC = 1024;
    
    [PluginParameter("IP Address of the\nOfficeLite VM")]
    private readonly string _iPAddress = "192.168.0.1";
        
    [PluginParameter("Default 50000")]
    private readonly int _port = 50000;
    
    [PluginParameter("Axis interpolation step time in milliseconds\n0 = no interpolation\nDefault value is 70")]
    private readonly int _interpolationTime = 70;

    private Client? _client;
    private readonly byte[] _sendData = new byte [DATA_SIZE_TO_KRC];
    private readonly byte[] _receiveData = new byte [DATA_SIZE_FROM_KRC];
    private readonly Interpolator[] _interpolator = new Interpolator[12];
    private readonly StopwatchEx _stopwatch = new();
    
    protected override bool OnSave()
    {
        InputStructure.AddVariable("Input", TcType.Byte, 1024);
        //InputStructure.AddVariable("StandardSafety", TcType.Dword);
        
        OutputStructure.AddVariable("Output", TcType.Byte, 1024);
        OutputStructure.AddVariable("Axis", TcType.Real, 12);
        //OutputStructure.AddVariable("Position", TcType.Real, 6);
        //OutputStructure.AddVariable("Tool", TcType.Real, 6);
        //OutputStructure.AddVariable("Base", TcType.Real, 6);
        //OutputStructure.AddVariable("StandardSafety", TcType.Dword);
        //OutputStructure.AddVariable("Sop1", TcType.Dword);
        //OutputStructure.AddVariable("Sop2", TcType.Dword);
        //OutputStructure.AddVariable("Reserve", TcType.Dword);
        
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
        _client = new Client(_iPAddress, _port);
        _client.Start();
        _client.OnConnected += ClientOnConnected;
        _client.OnServerMessage += ClientOnServerMessage;
        _client.OnError += message => Logger.LogWarning(this, message);
        for (var i = 0; i < 12; i++) _interpolator[i] = new Interpolator(_interpolationTime / 1000.0f);
        return true;
    }

    protected override void OnUpdate()
    {
        try
        {
            Array.Copy(InputBuffer, 0, _sendData, 0, 1024);
            //Array.Copy(InputBuffer, 1024, _sendData, 1024, 4);
            
            Array.Copy(_receiveData, 0, OutputBuffer, 0, 1024);
            AxisCopy();
            //Array.Copy(_receiveData, 1144, OutputBuffer, 1072, 16);
        }
        catch (Exception e)
        {
            Logger.LogError(this, e.Message, true);
            CancellationRequest();
        }
    }

    private void AxisCopy()
    {
        if (_interpolationTime == 0)
        {
            Array.Copy(_receiveData, 1024, OutputBuffer, 1024, 48);
            return;
        }
        
        for (var i = 0; i < 12; i++)
        {
            var offset = 1024 + i * 4;
            var rawValue = BitConverter.ToSingle(_receiveData, offset);
            var interpolatedValue = BitConverter.GetBytes((float)_interpolator[i].Calculate(rawValue));
            Array.Copy(interpolatedValue, 0, OutputBuffer, offset, 4);
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