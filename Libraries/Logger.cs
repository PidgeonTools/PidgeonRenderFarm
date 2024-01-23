using Libraries.Enums;

namespace Libraries;

/// <summary>
/// This class provides various functions for logging events
/// </summary>
public static class Logger
{
    private static bool Enable_Logging;
    private static LogLevel Log_Level;

    private static ConsoleColor Default_Foreground_Color;
    private static ConsoleColor Default_Background_Color;

    private static ConsoleColor Warn_Foreground_Color;
    private static ConsoleColor Warn_Background_Color;

    private static ConsoleColor Error_Foreground_Color;
    private static ConsoleColor Error_Background_Color;

    /// <summary>
    /// Init function for the Logger module
    /// </summary>
    /// <param name="enable_logging">Whether logging should be enabled</param>
    public static void Initialize(bool enable_logging, LogLevel level = LogLevel.Info)
    {
        Enable_Logging = enable_logging;
        Log_Level = level;

        Default_Foreground_Color = Console.ForegroundColor;
        if ((int)Default_Foreground_Color == -1)
        {
            Default_Foreground_Color = ConsoleColor.White;
        }
        Default_Background_Color = Console.BackgroundColor;
        if ((int)Default_Background_Color == -1)
        {
            Default_Background_Color = ConsoleColor.Black;
        }

        Warn_Foreground_Color = ConsoleColor.Black;
        Warn_Background_Color = ConsoleColor.Yellow;

        Error_Foreground_Color = ConsoleColor.White;
        Error_Background_Color = ConsoleColor.Red;
    }

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
        if (level < Log_Level)
        {
            return;
        }

        if (!silenced)
        {
            if (level >= LogLevel.Error)
            {
                Console.ForegroundColor = Error_Foreground_Color;
                Console.BackgroundColor = Error_Background_Color;
            }
            else if (level >= LogLevel.Warn)
            {
                Console.ForegroundColor = Warn_Foreground_Color;
                Console.BackgroundColor = Warn_Background_Color;
            }
            Console.WriteLine($"{level}: {message} @ {DateTime.Now}");

            Console.ForegroundColor = Default_Foreground_Color;
            Console.BackgroundColor = Default_Background_Color;
        }
        DBHandler.Insert_Log_Table(DateTime.Now.ToString(), level, module.ToString(), message);
    }
}