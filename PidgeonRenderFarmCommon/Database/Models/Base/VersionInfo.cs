namespace PidgeonRenderFarm.Common.Database.Models;

public class VersionInfo : BaseEntity
{
    public int Major { get; set; }
    public int Minor { get; set; }
    public int Patch { get; set; }
    public string? Info { get; set; }
    
    public string VersionString => $"{Major}.{Minor}.{Patch}{(string.IsNullOrEmpty(Info) ? "" : $"-{Info}")}";
    
    public virtual BlenderInstallation[]? BlenderInstallations { get; set; }

    public VersionInfo() { }
    public VersionInfo(string versionString)
    {
        VersionInfo version = Parse(versionString);
        Major = version.Major;
        Minor = version.Minor;
        Patch = version.Patch;
        Info = version.Info;
    }
    public VersionInfo(int major, int minor, int patch, string? info = null)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Info = info;
    }
    
    public static VersionInfo Parse(string versionString)
    {
        string[] numberSeparatedFromString = versionString.Split('-');
        string[] numbers = numberSeparatedFromString[0].Split('.');
        
        int newMajor = int.Parse(numbers[0]);
        int newMinor = int.Parse(numbers[1]);
        int newPatch = int.Parse(numbers[2]);
        string? newInfo = numberSeparatedFromString.Length > 1 ? numberSeparatedFromString[1] : null;
        
        VersionInfo version = new VersionInfo(newMajor, newMinor, newPatch, newInfo);

        return version;
    }

    public bool VerifyExactMatch(VersionInfo version)
    {
        return VerifyExactMatch(version.VersionString);
    }
    public bool VerifyExactMatch(string versionString)
    {
        if (VersionString == versionString)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    
    public bool VerifyCoarseMatch(string versionString)
    {
        VersionInfo version = VersionInfo.Parse(versionString);
        return VerifyCoarseMatch(version);
    }
    public bool VerifyCoarseMatch(VersionInfo version)
    {
        if (version.Major == Major && version.Minor == Minor)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}