using System.Text;
using OC.OfficeLiteServer;

var settings = new Settings().Read();
var server = new Y200Server(settings);
var cancel = new CancellationTokenSource();
var token = cancel.Token;

const string header =
    """
     ──────────────────────────────────────────────────
       OC.OfficeLiteServer (c)2026 Open Commissioning
     
       '/' to open the command menu
       ENTER to select, ESC to cancel
     ──────────────────────────────────────────────────
     
     """;

var commands = new (string Name, string? Argument, string Description)[]
{
    ("exit",       null,     "Exit the server"),
    ("restart",    null,     "Restart the server"),
    ("get-port",   null,     "Get the port the server is listening on"),
    ("set-port",   "<PORT>", "Set the port the server is listening on"),
    ("get-config", null,     "Get the configuration-path of the server"),
    ("set-config", "<PATH>", "Set the configuration-path of the server"),
};

Console.CancelKeyPress += (_, _) => cancel.Cancel();
Console.CursorVisible = false;
Console.Title = "OC.OfficeLiteServer";
Console.WriteLine(header);

_ = Task.Run(() =>
{
    while (!token.IsCancellationRequested)
    {
        var key = Console.ReadKey(true);
        if (key.KeyChar != '/') continue;

        var selectedIndex = ShowCommandMenu(commands);
        if (selectedIndex < 0) continue;

        var cmd = commands[selectedIndex];
        string? argument = null;

        if (cmd.Argument is not null)
        {
            argument = ReadArgument(cmd.Name);
            if (argument is null) continue;
        }

        try
        {
            switch (cmd.Name)
            {
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
                    settings.Port = int.Parse(argument!);
                    settings.Write();
                    RestartServer();
                    break;
                case "set-config":
                    settings.Config = argument!;
                    settings.Write();
                    RestartServer();
                    break;
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

static int ShowCommandMenu((string Name, string? Argument, string Description)[] commands)
{
    Console.WriteLine();
    for (var i = 0; i < commands.Length; i++) Console.WriteLine();
    var menuTop = Console.CursorTop - commands.Length;
    var headerTop = menuTop - 1;
    var selected = 0;
    DrawMenu(commands, menuTop, selected);

    try
    {
        while (true)
        {
            var input = Console.ReadKey(true);
            switch (input.Key)
            {
                case ConsoleKey.Escape:
                    ClearLines(headerTop, commands.Length + 1);
                    return -1;
                case ConsoleKey.UpArrow:
                    selected = (selected - 1 + commands.Length) % commands.Length;
                    DrawMenu(commands, menuTop, selected);
                    break;
                case ConsoleKey.DownArrow:
                    selected = (selected + 1) % commands.Length;
                    DrawMenu(commands, menuTop, selected);
                    break;
                case ConsoleKey.Enter:
                    ClearLines(headerTop, commands.Length + 1);
                    return selected;
            }
        }
    }
    finally
    {
        Console.CursorVisible = false;
    }
}

static void DrawMenu((string Name, string? Argument, string Description)[] commands, int top, int selected)
{
    for (var i = 0; i < commands.Length; i++)
    {
        Console.SetCursorPosition(0, top + i);
        var label = $" /{commands[i].Name} {commands[i].Argument}";
        var line = label.PadRight(22) + commands[i].Description;
        var width = Math.Max(1, Console.WindowWidth - 1);
        line = line.Length > width ? line.Substring(0, width) : line.PadRight(width);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        if (i == selected)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
        }
        Console.Write(line);
        Console.ResetColor();
    }
    Console.SetCursorPosition(0, top + commands.Length - 1);
}

static void ClearLines(int top, int count)
{
    var width = Math.Max(1, Console.WindowWidth - 1);
    for (var i = 0; i < count; i++)
    {
        var row = top + i;
        if (row < 0) continue;
        Console.SetCursorPosition(0, row);
        Console.Write(new string(' ', width));
    }
    Console.SetCursorPosition(0, Math.Max(0, top));
}

static string? ReadArgument(string commandName)
{
    var prefix = $"> /{commandName} ";
    Console.WriteLine();
    Console.Write(prefix);
    Console.CursorVisible = true;
    var buffer = new StringBuilder();

    try
    {
        while (true)
        {
            var input = Console.ReadKey(true);

            switch (input.Key)
            {
                case ConsoleKey.Escape:
                    Console.Write('\r');
                    Console.Write(new string(' ', prefix.Length + buffer.Length));
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    return null;
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    return buffer.ToString();
                case ConsoleKey.Backspace when buffer.Length == 0:
                    continue;
                case ConsoleKey.Backspace:
                    buffer.Length--;
                    Console.Write("\b \b");
                    continue;
            }

            if (char.IsControl(input.KeyChar)) continue;
            buffer.Append(input.KeyChar);
            Console.Write(input.KeyChar);
        }
    }
    finally
    {
        Console.CursorVisible = false;
    }
}