using System.IO;
using System.Text.Json;
using OC.Assistant.Sdk;

namespace OC.OfficeLiteServer;

public class Settings
{
    public int Port { get; set; } = 50000;
    public int IoSize { get; set; } = 1024;
    public string Config { get; set; } = @"C:\KRC\User\Y200Interface.config";
}

public static class SettingsExtension
{
    public static Settings Read(this Settings settings)
    {
        try
        {
            if (!File.Exists(App.SettingsPath))
            {
                settings.Write();
            }
            settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(App.SettingsPath)) ?? settings;
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(Settings), e.Message);
        }
        
        return settings;
    }
    
    public static void Write(this Settings settings)
    {
        try
        { 
            File.WriteAllText(App.SettingsPath, JsonSerializer.Serialize(settings));
        }
        catch (Exception e)
        { 
            Logger.LogError(typeof(Settings), e.Message);
        }
    }
}