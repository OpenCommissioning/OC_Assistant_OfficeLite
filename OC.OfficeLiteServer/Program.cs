using OC.OfficeLiteServer;

var settings = new Settings().Read();
var server = new Y200Server(settings);
var cancel = new CancellationTokenSource();
var token = cancel.Token;

const string header =
    """
     ──────────────────────────────────────────────
      OC.OfficeLiteServer ©2026 Open Commissioning    
     ──────────────────────────────────────────────
     /help to show commands

     """;

const string helpText = 
    """
    Commands:
    /help                Show this help
    /exit                Exit the server
    /restart             Restart the server
    /get-port            Get the port the server is listening on
    /set-port <PORT>     Set the port the server is listening on
    /get-config          Get the configuration-path of the server
    /set-config <PATH>   Set the configuration-path of the server
    """;

Console.CancelKeyPress += (_, _) => cancel.Cancel();
Console.CursorVisible = false;
Console.Title = "OC.OfficeLiteServer";
Console.WriteLine(header);

_ = Task.Run(async () =>
{
    while (!token.IsCancellationRequested)
    {
        var key = Console.ReadKey(true);
        if (key.KeyChar != '/') continue;
        
        Console.Write("\n> /");
        Console.CursorVisible = true;
        var line = await Console.In.ReadLineAsync();
        Console.CursorVisible = false;

        if (line is null) continue;
        
        var lines = line.Split(' ');

        try
        {
            switch (lines[0])
            {
                case "help":
                    Log.Info(helpText);
                    break;
                case "exit":
                    server.Stop();
                    cancel.Cancel();
                    break;
                case "restart":
                    RestartServer();
                    break;
                case "get-port":
                    Log.Info(settings.Port.ToString());
                    break;
                case "get-config":
                    Log.Info(settings.Config);
                    break;
                case "set-port":
                    settings.Port = int.Parse(lines[1]);
                    settings.Write();
                    RestartServer();
                    break;
                case "set-config":
                    settings.Config = lines[1];
                    settings.Write();
                    RestartServer();
                    break;
                default:
                    throw new Exception($"Invalid input '{lines[0]}'");
            }
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
        }
    }
});

try
{
    server.Start();
    await Task.Delay(-1, token);
}
catch
{
    // ignored
}

return;

void RestartServer()
{
    Log.Info("Restarting server...");
    server.Stop();
    server = new Y200Server(settings);
    server.Start();
}