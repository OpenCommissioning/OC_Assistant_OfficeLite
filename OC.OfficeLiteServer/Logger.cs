namespace OC.OfficeLiteServer;

internal static class Logger
{
    public static void LogInfo(object sender, string message) => Info?.Invoke(sender, message);
    public static void LogWarning(object sender, string message) => Warning?.Invoke(sender, message);
    public static void LogError(object sender, string message) => Error?.Invoke(sender, message);
    public static event Action<object, string>? Info;
    public static event Action<object, string>? Warning;
    public static event Action<object, string>? Error;
}