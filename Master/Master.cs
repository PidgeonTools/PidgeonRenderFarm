using Microsoft.VisualBasic.FileIO;
using System;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

class Master
{
    public static string SCRIPT_DIRECTORY = System.Reflection.Assembly.GetEntryAssembly().Location;
    public static string LOGS_DIRECTORY = "";
    public static string PROJECT_DIRECTORY = "";
    public static string PROJECT_EXTENSION = "prfp";
    public static string SETTINGS_FILE = "";

    //public Dictionary<string, dynamic> SETTINGS_OBJECT = new Dictionary<string, dynamic>();
    public static Settings SETTINGS;
    //public Dictionary<string, dynamic> PROJECT_OBJECT = new Dictionary<string, dynamic>();
    public static Project PROJECT;
    public static List<int> frames_left = new List<int>();

    static void Main(string[] args)
    {
        Console.WriteLine("Pidgeon Render Farm");
        Console.WriteLine("Join the Discord server for support - https://discord.gg/cnFdGQP");


        Main_Menu();


        /*Load_Settings();

        LOGS_DIRECTORY = Path.Join(SCRIPT_DIRECTORY, "Logs");
        if (!Directory.Exists(LOGS_DIRECTORY))
        {
            Directory.CreateDirectory(LOGS_DIRECTORY);
        }*/
    }

    public static void Main_Menu()
    {
        Console.Clear();

        int selected = 0;
        Console.CursorVisible = false;

        List<Menu_Item> items = new List<Menu_Item>();
        items.Add(new Menu_Item());
        items.Add(new Menu_Item());
        items.Add(new Menu_Item());

        items[0].text = "Help";
        items[0].selected = true;
        items[1].text = "New project";
        items[2].text = "Load project from disk";

        while (true)
        {
            Console.WriteLine("Pidgeon Render Farm");
            Console.WriteLine("Join the Discord server for support - https://discord.gg/cnFdGQP");
            Console.WriteLine("");
            Console.WriteLine("#--------------------------------------------------------------#");
            Console.WriteLine("");

            foreach (Menu_Item item in items)
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

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            ConsoleKey key = keyInfo.Key;

            if (key == ConsoleKey.UpArrow && selected != 0)
            {
                items[selected].selected = false;

                selected--;

                items[selected].selected = true;
            }

            else if (key == ConsoleKey.DownArrow && selected != 2)
            {
                items[selected].selected = false;

                selected++;

                items[selected].selected = true;
            }

            else if (key == ConsoleKey.Enter)
            {
                Console.Clear();
                Console.CursorVisible = true;
                break;
            }

            Console.Clear();
        }
    }

    #region Setup
    public static void First_Time_Setup()
    {
        Settings new_settings = new Settings();

        // Port
        Console.WriteLine("Which Port to use? (Default: 8080):");
        string user_input = Console.ReadLine();
        while (!Is_Port(user_input))
        {
            Console.WriteLine("Please input a whole number between 1 and 65536");
            user_input = Console.ReadLine();

            if (user_input == "")
            {
                user_input = "8080";
            }
        }
        new_settings.port = int.Parse(user_input);

        // Blender Executable
        Console.WriteLine("Where is you blender.exe stored? (It's recommended not to use blender-launcher.exe):");
        user_input = Console.ReadLine();
        while (!File.Exists(user_input))
        {
            Console.WriteLine("Please input the path to blender.exe");
            user_input = Console.ReadLine();
        }
        new_settings.blender_executable = user_input;

        // Keep output
        Console.WriteLine("Keep the files received from the clients? [y/N] (Default: True):");
        user_input = Console.ReadLine();
        while (Parse_Bool(user_input, true) == null)
        {
            Console.WriteLine("input y or n, alternatively leave empty for the default value");
            user_input = Console.ReadLine();
        }
        new_settings.keep_output = Parse_Bool(Console.ReadLine(), true);

        Save_Settings(new_settings);
    }

    public static void Save_Settings(Settings new_settings)
    {
        SETTINGS = new_settings;

        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(SETTINGS, options);

        File.WriteAllText(SETTINGS_FILE, jsonString);
    }

    public static void Load_Settings()
    {
        if (!File.Exists(SETTINGS_FILE))
        {
            First_Time_Setup();
            return;
        }

        string json_string = File.ReadAllText(SETTINGS_FILE);
        SETTINGS = JsonSerializer.Deserialize<Settings>(json_string);
    }
    #endregion

    #region Project_Setup
    public static void Project_Setup()
    {

    }

    public static void Save_Project(Project new_project)
    {
        PROJECT = new_project;

        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(PROJECT, options);

        File.WriteAllText(Path.Combine(PROJECT_DIRECTORY, PROJECT.id), jsonString);
    }

    public static void Load_Project(string project_file)
    {
        string json_string = File.ReadAllText(project_file);
        PROJECT = JsonSerializer.Deserialize<Project>(json_string);
        PROJECT_DIRECTORY = Path.Join(SCRIPT_DIRECTORY, PROJECT.id);

        if (!File.Exists(PROJECT.full_path_blend))
        {
            Environment.Exit(1);
        }
    }
    #endregion

    public static void Render_Project()
    {
        IPHostEntry host = Dns.GetHostEntry("localhost");
        IPAddress ipAddress = host.AddressList[0];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

        try
        {

            // Create a Socket that will use Tcp protocol
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            // A Socket must be associated with an endpoint using the Bind method
            listener.Bind(localEndPoint);
            // Specify how many requests a Socket can listen before it gives Server busy response.
            // We will listen 10 requests at a time
            listener.Listen(10);

            Console.WriteLine("Waiting for a connection...");
            Socket handler = listener.Accept();

            // Incoming data from the client.
            string data = "";
            byte[] bytes;

            while (true)
            {
                bytes = new byte[1024];
                int bytesRec = handler.Receive(bytes);
                data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                if (data.IndexOf("<EOF>") > -1)
                {
                    break;
                }
            }

            Console.WriteLine("Text received : {0}", data);

            byte[] msg = Encoding.ASCII.GetBytes(data);
            handler.Send(msg);
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }

        Console.WriteLine("\n Press any key to continue...");
        Console.ReadKey();
    }

    public static bool Is_Port(string port)
    {
        if (!int.TryParse(port, out _))
        {
            return false;
        }

        return (int.Parse(port) <= 1 && int.Parse(port) <= 65535);
    }

    public static dynamic Parse_Bool(string value, bool def = true)
    {
        if (new List<string>(){"true", "yes", "y", "1"}.Any(s => s.Contains(value.ToLower())))
        {
            return true;
        }

        else if (new List<string>() {"false", "no", "n", "0"}.Any(s => s.Contains(value.ToLower())))
        {
            return false;
        }

        else if (new List<string>() { "", null }.Any(s => s.Contains(value.ToLower())))
        {
            return def;
        }

        return null;
    }
}

public class Settings
{
    public int port { get; set; } = 8080;
    public string blender_executable { get; set; }
    public bool keep_output { get; set; }
}

public class Project
{
    public string id { get; set; }
    public string full_path_blend { get; set; }
    public string render_engine { get; set;}
    public string output_file_format { get; set; }
    public bool video_generate { get; set; }
    public int video_fps { get; set; }
    public string video_rate_control { get; set; } // CBR, CRF
    public int video_rate_control_value { get; set; }
    public bool video_resize { get; set; }
    public int video_x { get; set; }
    public int video_y { get; set; }
    public int Chunks { get; set; } = 0;
    public float time_per_frame { get; set; }
    public int first_frame { get; set; }
    public int last_frame { get; set; }
    public int frames_total { get; set; }
    public List<int> frames_complete { get; set; }
}

public class Menu_Item
{
    public string text { get; set; }
    public bool selected { get; set; } = false;
}