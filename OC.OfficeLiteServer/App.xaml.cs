namespace OC.OfficeLiteServer;

public abstract class App
{
    private static readonly string Folder = 
        $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\OC.OfficeLiteServer";
    private static string Path => System.IO.Directory.CreateDirectory(Folder).FullName;
    public static string SettingsPath => $"{Path}\\settings.json";
    public static string LogFilePath => $"{Path}\\log.txt";
}