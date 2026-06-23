using System.Reflection;

namespace OC.OfficeLiteServer;

public class Settings
{
    public static readonly AssemblyName AppName = Assembly.GetExecutingAssembly().GetName();
    
    private static readonly string Folder = 
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName.Name);

    private static readonly string SettingsPath = 
        Path.Combine(Directory.CreateDirectory(Folder).FullName, "settings.ini");
    
    public int Port { get; set; } = 50000;
    public string Config { get; set; } = @"C:\KRC\User\Y200Interface.config";
    
    public Settings Read()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                Write();
                return this;
            }

            foreach (var line in File.ReadAllLines(SettingsPath))
            {
                var separator = line.IndexOf('=');
                if (separator < 0)
                {
                    continue;
                }

                var key = line.Substring(0, separator).Trim();
                var value = line.Substring(separator + 1).Trim();

                switch (key)
                {
                    case nameof(Port) when int.TryParse(value, out var port):
                        Port = port;
                        break;
                    case nameof(Config):
                        Config = value;
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
        }
        
        return this;
    }

    public void Write()
    {
        try
        { 
            File.WriteAllLines(SettingsPath,
            [
                $"{nameof(Port)}={Port}",
                $"{nameof(Config)}={Config}",
            ]);
        }
        catch (Exception e)
        { 
            Log.Error(e.Message);
        }
    }
}