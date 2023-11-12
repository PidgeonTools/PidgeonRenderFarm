public static class Logger
{
    public static bool Enable_Logging;

    public static void Log(object caller, string message, string level = "Info", bool silenced = false)
    {
        if (!silenced)
        {
            Console.WriteLine($"{level}: {message} @ {DateTime.Now}");
        }
        DBHandler.Insert_Log_Table(DateTime.Now.ToString(), level, caller.ToString(), message);
    }
}