namespace Libraries.Models;

public class SystemInfo
{
    public string PRF_Version { get; set; }
    public string OS_Description { get; set; }
    public string OS_Version { get; set; }
    public string OS_Architecture { get; set; }
    public int CPU_Count { get; set; }
    public int GPU_Count { get; set; }
    public int RAM { get; set; }

    public SystemInfo(bool allow_collection, string bin_directory)
    {
        if (allow_collection)
        {
            PRF_Version = "1.0.0-beta";
            OS_Description = DataCollector.Get_OS_Description();
            OS_Version = DataCollector.Get_OS_Version();
            OS_Architecture = DataCollector.Get_OS_Architecture();
            CPU_Count = DataCollector.Get_CPU_Count();
            GPU_Count = DataCollector.Get_GPU_Count();
            RAM = DataCollector.Get_RAM();
        }
        else
        {
            PRF_Version = "No data";
            OS_Description = "No data";
            OS_Version = "No data";
            OS_Architecture = "No data";
            CPU_Count = 0;
            GPU_Count = 0;
            RAM = 0;
        }

        FileHandler.Save_SystemInfo(Path.Join(bin_directory, "SystemInfo.json"), this);
    }
}