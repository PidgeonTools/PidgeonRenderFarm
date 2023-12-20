using Libraries.Enums;

namespace Libraries;

/// <summary>
/// This class provides various functions for logging events
/// </summary>
public static class Logger
{
    public static bool Enable_Logging;
    public static LogLevel LogLevel = LogLevel.Info;

    /// <summary>
    /// Log the entry if logging is enabledm
    /// </summary>
    /// <param name="module"></param>
    /// <param name="message"></param>
    /// <param name="level"></param>
    /// <param name="silenced"></param>
    public static void Log(object module, string message, LogLevel level = LogLevel.Info, bool silenced = false)
    {
        if (!Enable_Logging)
        {
            return;
        }
        if (level < LogLevel)
        {
            return;
        }

        if (!silenced)
        {
            Console.WriteLine($"{level}: {message} @ {DateTime.Now}");
        }
        DBHandler.Insert_Log_Table(DateTime.Now.ToString(), level, module.ToString(), message);
    }
}