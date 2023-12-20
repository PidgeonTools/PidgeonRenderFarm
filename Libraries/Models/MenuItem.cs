namespace Libraries.Models;

public class MenuItem
{
    public string text { get; set; }
    public bool selected { get; set; } = false;

    public MenuItem(string item_text = "")
    {
        text = item_text;
    }
}