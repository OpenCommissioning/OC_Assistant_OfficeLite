using System.Net.NetworkInformation;
using OC.Assistant.Sdk.Plugin;
using OC.Assistant.Sdk;

namespace OC.OfficeLite;

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
        
    [PluginParameter("Port of the Y200_Svr.exe\nDefault 40000")]
    private readonly int _port = 40000;

    [PluginParameter("Y200IpcName from\nC:\\KRC\\User\\Y200Interface.config")]
    private readonly string _y200IpcName = "Y200_VRC_R1";

    [PluginParameter("Y200WriteOffset from\nC:\\KRC\\User\\Y200Interface.config")]
    private readonly int _y200WriteOffset = 1024;

    [PluginParameter("Y200ReadOffset from\nC:\\KRC\\User\\Y200Interface.config")]
    private readonly int _y200ReadOffset = 0;

    [PluginParameter("Axis interpolation step time in milliseconds\n0 = no interpolation\nDefault value is 70")]
    private readonly int _interpolationTime = 70;

    private Y200Client? _client;
    private readonly byte[] _receiveData = new byte [DATA_SIZE_FROM_KRC];
    private readonly Interpolator[] _interpolator = new Interpolator[12];
    
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
            
        if (_y200WriteOffset < 0 || _y200ReadOffset < 0)
        {
            Logger.LogError(this, "Y200WriteOffset and Y200ReadOffset must not be negative");
            return false;
        }

        _client = new Y200Client(_iPAddress, _port, _y200IpcName,
            (uint)_y200WriteOffset, DATA_SIZE_FROM_KRC,
            (uint)_y200ReadOffset, DATA_SIZE_TO_KRC);
        _client.OnConnected += ClientOnConnected;
        _client.OnReadData += ClientOnReadData;
        _client.OnError += message => Logger.LogWarning(this, message);
        for (var i = 0; i < 12; i++) _interpolator[i] = new Interpolator(_interpolationTime / 1000.0f);
        _client.Start();
        return true;
    }

    protected override void OnUpdate()
    {
        try
        {
            _client?.SetWriteData(InputBuffer);

            lock (_receiveData)
            {
                Array.Copy(_receiveData, 0, OutputBuffer, 0, 1024);
                AxisCopy();
                //Array.Copy(_receiveData, 1144, OutputBuffer, 1072, 16);
            }
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
            _client?.Dispose();
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

    private void ClientOnReadData(byte[] buffer)
    {
        lock (_receiveData)
        {
            Array.Copy(buffer, 0, _receiveData, 0, DATA_SIZE_FROM_KRC);
        }
    }

    private void ClientOnConnected()
    {
        Logger.LogInfo(this, $"Connected to Y200 server {_iPAddress}:{_port}, process image '{_y200IpcName}'");
    }
}