namespace SharedFolderApp.Server.Logger
{
    public static class ConsoleLogger
    {
        public static void LogInfo(string message)
        {
            string date = DateTime.UtcNow.ToString("HH:mm:ss dd.MM.yyyy");
            Console.WriteLine($"[{date}] [INFO] {message}");
        }

        public static void LogError(string message)
        {
            string date = DateTime.UtcNow.ToString("HH:mm:ss dd.MM.yyyy");
            Console.Error.WriteLine($"[{date}] [ERROR] {message}");
        }
    }
}
