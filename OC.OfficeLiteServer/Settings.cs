using System.Text.Json;

namespace OC.OfficeLiteServer;

public class Settings
{
    public int Port { get; set; } = 50000;
    public string Config { get; set; } = @"C:\KRC\User\Y200Interface.config";
}

public static class SettingsExtension
{
    private static readonly string Folder = 
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OC.OfficeLiteServer");

    private static readonly string SettingsPath = 
        Path.Combine(Directory.CreateDirectory(Folder).FullName, "settings.json");
    
    extension(Settings settings)
    {
        public Settings Read()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    settings.Write();
                }
                settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsPath)) ?? settings;
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        
            return settings;
        }

        public void Write()
        {
            try
            { 
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings));
            }
            catch (Exception e)
            { 
                Log.Error(e.Message);
            }
        }
    }
}