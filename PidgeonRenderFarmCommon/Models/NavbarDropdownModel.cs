namespace PidgeonRenderFarm.Common.Models;

public class NavbarDropdownModel
{
    public string Header { get; set; }
    public string? HeaderLink { get; set; }
    
    public Dictionary<string, string> Items { get; set; }
    
    public NavbarDropdownModel() {}

    public NavbarDropdownModel(string header, Dictionary<string, string> items, string? headerLink = null)
    {
        Header = header;
        Items = items;
        HeaderLink = headerLink;
    }
}