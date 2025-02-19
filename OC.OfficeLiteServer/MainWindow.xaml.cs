using System.ComponentModel;
using System.Windows;
using OC.Assistant.Sdk;
using OC.Assistant.Theme;

namespace OC.OfficeLiteServer;

public partial class MainWindow
{
    private Y200Server? _server;
        
    public MainWindow()
    {
        InitializeComponent();
        LogViewer.LogFilePath = App.LogFilePath;
        Logger.Info += (sender, message) => LogViewer.Add(sender, message, MessageType.Info);
        Logger.Warning += (sender, message) => LogViewer.Add(sender, message, MessageType.Warning);
        Logger.Error += (sender, message) => LogViewer.Add(sender, message, MessageType.Error);
        Task.Run(() =>
        {
            _server = new Y200Server( new Settings().Read());
            _server.Start();
        });
    }
    
    private void MainWindow_OnClosing(object sender, CancelEventArgs e)
    {
        _server?.Stop();
    }

    private void SettingsOnClick(object sender, RoutedEventArgs e)
    {
        var settings = new SettingsView();
        
        var result = Assistant.Theme.MessageBox
            .Show("Settings", settings, MessageBoxButton.OKCancel, MessageBoxImage.None);

        if (result != MessageBoxResult.OK || !settings.Apply()) return;
        
        Logger.LogInfo(this, "Restarting server...");
        _server?.Stop();
        _server = new Y200Server(settings.Settings);
        _server.Start();
    }
}