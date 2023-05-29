using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

class Master
{
    public static string SCRIPT_DIRECTORY = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
    public static string LOGS_DIRECTORY = "";
    public static string LOGS_FILE = "";
    public static string PROJECT_DIRECTORY = "";
    public static string PROJECT_EXTENSION = "prfp";
    public static string SETTINGS_FILE = "";
    public static string DATA_FILE = "";

    public static Settings SETTINGS;
    public static PRF_Data PRF_DATA;
    public static Project PROJECT;
    public static List<int> frames_left = new List<int>();

    static void Main(string[] args)
    {
        DateTime start_time = DateTime.Now;

        LOGS_DIRECTORY = Path.Join(SCRIPT_DIRECTORY, "logs");
        if (!Directory.Exists(LOGS_DIRECTORY))
        {
            Directory.CreateDirectory(LOGS_DIRECTORY);
        }

        LOGS_FILE = Path.Join(LOGS_DIRECTORY, ("master_" + start_time.ToString("HHmmss") + ".txt"));
        SETTINGS_FILE = Path.Join(SCRIPT_DIRECTORY, "master_settings.json");
        DATA_FILE = Path.Join(SCRIPT_DIRECTORY, "master_data.json");

        Load_Settings();
        Collect_Data();

        Show_Top_Bar();

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
        List<string> items = new List<string>
        {
            "New project",
            "Load project from disk",
            "Re-run setup",
            "Get help on Discord",
            "Donate - Out of order",
            "Exit"
        };
        string selection = Menu(items, new List<string> { });

        if (selection == items[0])
        {

        }

        else if (selection == items[1])
        {

        }

        else if (selection == items[2])
        {
            First_Time_Setup();
            Main_Menu();
        }

        else if (selection == items[3])
        {
            Process.Start(new ProcessStartInfo("https://discord.gg/cnFdGQP") { UseShellExecute = true });
            Main_Menu();
        }

        else if (selection == items[4])
        {

        }

        else
        {
            Environment.Exit(1);
        }
    }

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

    public static string Menu(List<string> options, List<string> headlines)
    {
        int selected = 0;
        Console.CursorVisible = false;

        List<Menu_Item> items = new List<Menu_Item>();

        foreach (string option in options)
        {
            items.Add(new Menu_Item(option));
        }

        items[0].selected = true;

        while (true)
        {
            Console.Clear();
            Show_Top_Bar();

            if (headlines.Count != 0)
            {
                foreach (string headline in headlines)
                {
                    Console.WriteLine(headline);
                }
                //Console.WriteLine("#--------------------------------------------------------------#");
                //Console.WriteLine("");
            }

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

            else if (key == ConsoleKey.DownArrow && selected != (items.Count() - 1))
            {
                items[selected].selected = false;

                selected++;

                items[selected].selected = true;
            }

            else if (key == ConsoleKey.Enter)
            {
                Console.Clear();
                Console.CursorVisible = true;
                return items[selected].text;
            }
        }
    }

    public static void Show_Top_Bar()
    {
        Console.WriteLine("Pidgeon Render Farm");
        Console.WriteLine("Join the Discord server for support - https://discord.gg/cnFdGQP");
        Console.WriteLine("");
        Console.WriteLine("#--------------------------------------------------------------#");
        Console.WriteLine("");
    }

    #region Setup
    public static void First_Time_Setup()
    {
        Settings new_settings = new Settings();

        // Port
        Show_Top_Bar();
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
        Console.Clear();

        // Blender Executable
        Show_Top_Bar();
        Console.WriteLine("Where is you blender.exe stored? (It's recommended not to use blender-launcher.exe)");
        user_input = Console.ReadLine();
        while (!File.Exists(user_input))
        {
            Console.WriteLine("Please input the path to blender.exe (Without '')");
            user_input = Console.ReadLine();
        }
        new_settings.blender_executable = user_input;

        // Keep output
        new_settings.keep_output = Parse_Bool(Menu(new List<string> { "Yes", "No" },
                                                   new List<string> { "Keep the files received from the clients?" }));

        // Data collection
        new_settings.collect_data = Parse_Bool(Menu(new List<string> { "Yes", "No" },
                                                   new List<string> { "Allow us to collect data? (We have no acess to it, even if you enter yes!) " }));

        Save_Settings(new_settings);
    }

    public static void Save_Settings(Settings new_settings)
    {
        SETTINGS = new_settings;

        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(SETTINGS, options);

        //File.Create(SETTINGS_FILE);
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
        Project new_project = new Project();

        new_project.id = (PRF_DATA.projects + 1).ToString();

        // Blend file
        Show_Top_Bar();
        Console.WriteLine("Where .blend stored? (Be sure to actually use a .blend)");
        string user_input = Console.ReadLine();
        while (!File.Exists(user_input) && !user_input.EndsWith(".blend"))
        {
            Console.WriteLine("Please input the path to your .blend (Without '')");
            user_input = Console.ReadLine();
        }
        new_project.full_path_blend = user_input;

        bool test_render = Parse_Bool(Menu(new List<string> { "Yes", "No" },
                                                   new List<string> { "Render a test frame? (Will take some time, for client option 'Maximum time per frame')" }));

        new_project.video_generate = Parse_Bool(Menu(new List<string> { "Yes", "No" },
                                                   new List<string> { "Generate a video file? (MP4-Format, FFMPEG has to be installed!)" }));

        if (new_project.video_generate)
        {
            new_project.video_rate_control = Menu(new List<string> { "CRF", "CBR" },
                                                   new List<string> { "What Video Rate Control to use?" });

            // Value
            Show_Top_Bar();
            Console.WriteLine("Video Rate Control Value: (CRF - lower is better; CBR - higher is better)");
            user_input = Console.ReadLine();
            while (!int.TryParse(user_input, out _))
            {
                Console.WriteLine("Please input a whole positive number");
                user_input = Console.ReadLine();
            }
            new_project.video_rate_control_value = int.Parse(user_input);
            Console.Clear();

            new_project.video_resize = Parse_Bool(Menu(new List<string> { "Yes", "No" },
                                                   new List<string> { "Rescale the video?" }));

            // Res X
            Show_Top_Bar();
            Console.WriteLine("New video width:");
            user_input = Console.ReadLine();
            while (!int.TryParse(user_input, out _))
            {
                Console.WriteLine("Please input a whole positive number");
                user_input = Console.ReadLine();
            }
            new_project.video_x = int.Parse(user_input);
            Console.Clear();

            // Res Y
            Show_Top_Bar();
            Console.WriteLine("New video height:");
            user_input = Console.ReadLine();
            while (!int.TryParse(user_input, out _))
            {
                Console.WriteLine("Please input a whole positive number");
                user_input = Console.ReadLine();
            }
            new_project.video_y = int.Parse(user_input);
            Console.Clear();

            // Chunks
            Show_Top_Bar();
            Console.WriteLine("Chunk size:");
            user_input = Console.ReadLine();
            while (!int.TryParse(user_input, out _))
            {
                Console.WriteLine("Please input a whole positive number");
                user_input = Console.ReadLine();
            }
            new_project.video_y = int.Parse(user_input);
            Console.Clear();
        }

        PROJECT_DIRECTORY = Path.Join(SCRIPT_DIRECTORY, new_project.id);
        if (!Directory.Exists(PROJECT_DIRECTORY))
        {
            Directory.CreateDirectory(PROJECT_DIRECTORY);
        }

        //string command = SETTINGS.blender_executable;
        string command = "-b ";
        command += new_project.full_path_blend;
        command += " -P ";
        command += "BPY.py";
        command += " -- ";
        command += PROJECT_DIRECTORY;
        if (test_render)
        {
            command += " 1";
        }
        else
        {
            command += " 0";
        }

        Process.Start(SETTINGS.blender_executable, command);

        string json_string = File.ReadAllText(Path.Join(PROJECT_DIRECTORY, "vars.json"));
        Project_Data project_data = JsonSerializer.Deserialize<Project_Data>(json_string);

        new_project.render_engine = project_data.render_engine;
        new_project.time_per_frame = project_data.render_time;
        new_project.output_file_format = project_data.file_format;
        new_project.first_frame = project_data.first_frame;
        new_project.last_frame = project_data.last_frame;
        new_project.frames_total = project_data.last_frame - (project_data.first_frame - 1);

        for (int frame = new_project.first_frame; frame <= new_project.last_frame; frame++)
        {
            frames_left.Add(frame);
        }

        Save_Project(new_project);
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

    #region Data Handeling
    public static void Write_Log(string content)
    {
        if (!File.Exists(LOGS_FILE))
        {
            File.Create(LOGS_FILE).Close();
        }

        content += Environment.NewLine;

        File.AppendAllText(LOGS_FILE, content);
    }

    public static void Collect_Data()
    {
        if (SETTINGS.collect_data)
        {
            PRF_Data new_data = new PRF_Data();

            new_data.os = System.Runtime.InteropServices.RuntimeInformation.OSDescription;

            Save_Data(new_data);
        }
    }

    public static void Save_Data(PRF_Data new_data)
    {
        if (!File.Exists(DATA_FILE))
        {
            File.Create(DATA_FILE).Close();
            File.WriteAllText(DATA_FILE, JsonSerializer.Serialize(new_data));
            return;
        }

        PRF_DATA.projects += new_data.projects;

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json_string = JsonSerializer.Serialize(PRF_DATA, options);

        File.WriteAllText(DATA_FILE, json_string);
    }

    public static void Load_Data(PRF_Data new_data)
    {
        Save_Data(new PRF_Data());

        string json_string = File.ReadAllText(DATA_FILE);
        PRF_DATA = JsonSerializer.Deserialize<PRF_Data>(json_string);
    }
    #endregion

    #region Helpers
    public static bool Is_Port(string port)
    {
        if (!int.TryParse(port, out _))
        {
            return false;
        }

        return (int.Parse(port) >= 1 && int.Parse(port) <= 65535);
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
    #endregion
}

#region Objects
public class Settings
{
    public int port { get; set; } = 8080;
    public string blender_executable { get; set; }
    public bool keep_output { get; set; }
    public bool collect_data { get; set; }
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
    public List<int> frames_complete { get; set; } = new List<int>();
}

public class Project_Data
{
    public string render_engine { get; set; }
    public int render_time { get; set; } = 0;
    public string file_format { get; set; }
    public int first_frame { get; set; }
    public int last_frame { get; set; }
}

public class PRF_Data
{
    public string os { get; set; } = "No data";
    public int projects { get; set; } = 0;
}

public class Menu_Item
{
    public string text { get; set; }
    public bool selected { get; set; } = false;

    public Menu_Item(string item_text = "")
    {
        text = item_text;
    }
}
#endregion