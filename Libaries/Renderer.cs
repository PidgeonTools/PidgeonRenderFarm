public class Display
{
    // Print the top bar
    public static void Show_Top_Bar(string module, List<string>? addition = null)
    {
        Console.WriteLine($"Pidgeon Render Farm - {module}");
        Console.WriteLine($"Join the Discord server for support - https://discord.gg/cnFdGQP");
        Console.WriteLine("");

        if (addition != null)
        {
            foreach (string line in addition)
            {
                Console.WriteLine(line);
            }

            Console.WriteLine("");
        }

        Console.WriteLine("#--------------------------------------------------------------#");
        Console.WriteLine("");
    }

    public static void Update_View(string module, string ip_address_string, int port, string progress_bar)
    {
        Console.Clear();
        /*Show_Top_Bar(module, new List<string> { $"Master IP address: {ip_address_string}",
                                                $"Master Port: {port}",
                                                progress_bar});*/

        Show_Top_Bar(module, new List<string> { $"Master IP address: {ip_address_string}",
                                                $"Master Port: {port}" });

        /*foreach (string line in File.ReadLines(BACKUP_FILE))
        {
            Console.WriteLine(line);
        }*/
    }
}
public class Menu
{
    private List<string> Options;
    private List<string> Headlines;
    private string Module;

    public Menu(List<string> options, string module, List<string>? headlines = null)
    {
        Options = options;
        Headlines = headlines;
        Module = module;
    }

    // Open a semi-graphical menu allowing easy user input
    public string Show()
    {
        // Hide cursor
        int selected = 0;
        Console.CursorVisible = false;

        // Create and fill list with given options
        List<MenuItem> items = new List<MenuItem>();
        foreach (string option in Options)
        {
            items.Add(new MenuItem(option));
        }
        // Make the 1st option the default
        items[0].selected = true;

        // Infinite loop
        while (true)
        {
            // Clear everything and show the top bar
            Console.Clear();
            Display.Show_Top_Bar(Module);

            // Show an additional promt if given
            if (Headlines != null)
            {
                // Print every line
                foreach (string headline in Headlines)
                {
                    Console.WriteLine(headline);
                }
                //Console.WriteLine("#--------------------------------------------------------------#");
                //Console.WriteLine("");
            }

            // GO through all items and color them depending on the selection
            foreach (MenuItem item in items)
            {
                if (item.selected)
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.White;
                }

                Console.WriteLine(item.text);

                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;
            }

            // Wait for user input
            ConsoleKeyInfo keyInfo = Console.ReadKey();
            ConsoleKey key = keyInfo.Key;

            // If possible go up in list
            switch (key)
            {
                case ConsoleKey.UpArrow when selected != 0:
                    items[selected].selected = false;

                    selected--;

                    items[selected].selected = true;
                    break;

                // If possible go down in list
                case ConsoleKey.DownArrow when selected != (items.Count() - 1):
                    items[selected].selected = false;

                    selected++;

                    items[selected].selected = true;
                    break;

                // Return the current selection and break out of the loop
                // Show the cursor
                case ConsoleKey.Enter:
                    Console.Clear();
                    Console.CursorVisible = true;
                    return items[selected].text;
            }
        }
    }
}