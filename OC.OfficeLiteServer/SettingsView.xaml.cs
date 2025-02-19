namespace OC.OfficeLiteServer;

public partial class SettingsView
{
    public SettingsView()
    {
        InitializeComponent();
        Settings = new Settings().Read();
        Input = Settings;
    }
    
    public Settings Settings { get; }

    private Settings Input
    {
        get =>
            new()
            {
                Port = int.TryParse(PortInput.Text, out var port) ? port : Settings.Port,
                IoSize = int.TryParse(IoSizeInput.Text, out var ioSize) ? ioSize : Settings.IoSize,
                Config = Config.Text
            };
        set
        {
            PortInput.Text = value.Port.ToString();
            IoSizeInput.Text = value.IoSize.ToString();
            Config.Text = value.Config;
        }
    }
    
    public bool Apply()
    {
        var input = Input;
        
        if (input.Port == Settings.Port &&
            input.IoSize == Settings.IoSize &&
            input.Config == Settings.Config)
        {
            return false;
        }

        Settings.Port = input.Port;
        Settings.IoSize = input.IoSize;
        Settings.Config = input.Config;
        Settings.Write();
        return true;
    }
}