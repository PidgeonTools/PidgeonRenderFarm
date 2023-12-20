using Libraries.Models;

namespace Libraries;

/// <summary>
/// This class provides various functions for rendering to the command line
/// </summary>
public class Display
{
    /// <summary>
    /// This functions displays the top bar
    /// </summary>
    /// <param name="module">Module to display in the first line - either Client or Master</param>
    /// <param name="addition">Additional lines to print</param>
    public static void Show_Top_Bar(string module, List<string>? addition = null)
    {
        Console.WriteLine($"Pidgeon Render Farm - {module}");
        Console.WriteLine($"Join the Discord server for support - https://discord.gg/cnFdGQP");
        Console.WriteLine("");

        // If additions are provided
        if (addition != null && addition.Count != 0)
        {
            // -> print them as a seperate line
            foreach (string line in addition)
            {
                Console.WriteLine(line);
            }

            Console.WriteLine("");
        }

        Console.WriteLine("#--------------------------------------------------------------#");
        Console.WriteLine("");
    }
}

/// <summary>
/// This class provides functions to display a Menu that allows the user to easily pick from options
/// </summary>
public class Menu
{
    #region Properties
    private List<string> Options;
    private List<string> Headlines;
    private string Module;

    private ConsoleColor Default_Foreground_Color;
    private ConsoleColor Default_Background_Color;
    #endregion

    /// <summary>
    /// Initialize the Menu class
    /// </summary>
    /// <param name="options">The options the user can pick from</param>
    /// <param name="module">The module to display in the top bar</param>
    /// <param name="headlines"></param>
    public Menu(List<string> options, string module, List<string>? headlines = null)
    {
        Options = options;
        Headlines = headlines;
        Module = module;

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
    }

    /// <summary>
    /// Open the semi-graphical menu
    /// </summary>
    /// <returns>Return the selection as a string</returns>
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
                    Console.ForegroundColor = Default_Background_Color;
                    Console.BackgroundColor = Default_Foreground_Color;
                }

                Console.WriteLine(item.text);

                Console.ForegroundColor = Default_Foreground_Color;
                Console.BackgroundColor = Default_Background_Color;
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