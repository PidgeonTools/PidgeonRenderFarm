using Libraries.Enums;

namespace Libraries.Models.Database;

public class LogEntry
{
    public int Id { get; set; }
    public DateTime Time { get; set; }
    public LogLevel Level { get; set; }
    public string Module { get; set; }
    public string Message { get; set; }
}