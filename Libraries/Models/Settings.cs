namespace Libraries.Models;

public class Settings
{
    public List<Blender> Blender_Installations { get; set; }
    public bool Allow_Data_Collection { get; set; }
    //public bool Enable_Analytics { get; set; } = false;
    public bool Enable_Logging { get; set; }
    public DBConnection? Database_Connection { get; set; }
}

public class MasterSettings : Settings
{
    public string? IPv4_Overwrite { get; set; } = null;
    public int Port { get; set; } = 19186;
    public int Client_Limit { get; set; } = 0;
    public string? FFmpeg_Executable { get; set; }
    public FTPConnection FTP_Connection { get; set; }
    public SMBConnection SMB_Connection { get; set; }
    public bool Allow_Computation { get; set; }
}

public class ClientSettings : Settings
{
    public List<MasterConnection> Master_Connections { get; set; }
    public int RAM_Use_Limit { get; set; }
    public float Render_Time_Limit { get; set; }
    public bool Keep_Input { get; set; }
    public bool Keep_Output { get; set; }
    public bool Keep_ZIP { get; set; }
    public AutoShutdown Shutdown_Parameters { get; set; }
}

public class Blender
{
    public string Version { get; set; }
    public string Executable { get; set; }
    public string Render_Device { get; set; }
    public List<string> Allowed_Render_Engines { get; set; }
    public int CPU_Thread_Limit { get; set; }
}

public class AutoShutdown
{
    public int Rendered_Frames { get; set; } = 0;
    // 1 Second = 10000000 Ticks
    public TimeSpan Time { get; set; } = new TimeSpan(0);
    public int Failed_Connections { get; set; } = 0;
}