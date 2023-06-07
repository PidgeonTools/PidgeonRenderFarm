using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Text.Json;
using System.IO.Compression;

class Client
{
    #region Global Variables
    // Initialize global variables
    // Initialize global File names, directories
    public static string SCRIPT_DIRECTORY = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
    public static string LOGS_DIRECTORY = "";
    public static string LOGS_FILE = "";
    public static string PROJECT_DIRECTORY = "";
    public static string SETTINGS_FILE = "";
    public static string DATA_FILE = "";

    // Create global objects and variables
    public static float VERSION = 1.0f;
    public static Settings SETTINGS;
    public static PRF_Data PRF_DATA;
    #endregion

    // Runs at start
    static void Main(string[] args)
    {
        // log the start time
        DateTime start_time = DateTime.Now;

        // Get log directory name and create it
        LOGS_DIRECTORY = Path.Join(SCRIPT_DIRECTORY, "logs");
        if (!Directory.Exists(LOGS_DIRECTORY))
        {
            Directory.CreateDirectory(LOGS_DIRECTORY);
        }

        // Get the name for the current log, settings and data
        LOGS_FILE = Path.Join(LOGS_DIRECTORY, ("client_" + start_time.ToString("HHmmss") + ".txt"));
        SETTINGS_FILE = Path.Join(SCRIPT_DIRECTORY, "client_settings.json");
        DATA_FILE = Path.Join(SCRIPT_DIRECTORY, "client_data.json");

        // Main loop start
        Load_Settings();
        Collect_Data();
        Main_Menu();
    }

    public static void Main_Menu()
    {
        // Create list with all options and hand it to Menu()
        List<string> items = new List<string>
        {
            "Start Client",
            "Re-run setup",
            "Visit documentation",
            "Get help on Discord",
            "Donate - Out of order",
            "Exit"
        };

        while (true)
        {
            // Show the Menu and grab the selection
            string selection = Menu(items, new List<string> { });

            // Compare selection with options and execute function
            if (selection == items[0])
            {

            }

            else if (selection == items[1])
            {
                // Run the first time setup and come back here
                First_Time_Setup();
            }

            else if (selection == items[2])
            {
                // Open documentation on Github.com
                Process.Start(new ProcessStartInfo("https://github.com/PidgeonTools/PidgeonRenderFarm") { UseShellExecute = true });
                Main_Menu();
            }

            else if (selection == items[3])
            {
                // Open Discord invite in browser
                Process.Start(new ProcessStartInfo("https://discord.gg/cnFdGQP") { UseShellExecute = true });
                Main_Menu();
            }

            else if (selection == items[4])
            {

            }

            else
            {
                // Exit PRF
                Environment.Exit(1);
            }
        }
    }

    public static void Worker()
    {
        while (true)
        {
            try
            {
                IPAddress ip_address = IPAddress.Parse(SETTINGS.masters[0].ip);
                IPEndPoint remote_end_point = new IPEndPoint(ip_address, SETTINGS.masters[0].port);
                Socket connection = new Socket(ip_address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    // Connect to Remote EndPoint
                    connection.Connect(remote_end_point);

                    DateTime connection_time = DateTime.Now;
                    Console.WriteLine("Connected to: " + connection.RemoteEndPoint.ToString() + " @ " + connection_time.ToString("HH:mm:ss"));
                    Write_Log("Connected to: " + connection.RemoteEndPoint.ToString() + " @ " + connection_time.ToString("HH:mm:ss"));

                    Client_Response client_response = new Client_Response();

                    client_response.message = "new";
                    string json_send = JsonSerializer.Serialize(client_response);
                    byte[] bytes_send = Encoding.UTF8.GetBytes(json_send);
                    connection.Send(bytes_send);

                    byte[] buffer = new byte[1024];
                    int received = connection.Receive(buffer);
                    string json_receive = Encoding.UTF8.GetString(buffer, 0, received);
                    Master_Response master_response = JsonSerializer.Deserialize<Master_Response>(json_receive);

                    client_response = new Client_Response();

                    PROJECT_DIRECTORY = Path.Join(SCRIPT_DIRECTORY, master_response.id);
                    string blend_file = Path.Join(PROJECT_DIRECTORY, (master_response.id + ".blend"));
                    long file_size = new FileInfo(blend_file).Length;

                    if (!File.Exists(blend_file) || file_size != master_response.file_size)
                    {
                        client_response.message = "needed";
                        json_send = JsonSerializer.Serialize(client_response);
                        bytes_send = Encoding.UTF8.GetBytes(json_send);
                        connection.Send(bytes_send);

                        string path = Path.Join(PROJECT_DIRECTORY, blend_file);
                        using (FileStream file_stream = File.Create(path))
                        {
                            new NetworkStream(connection).CopyTo(file_stream);
                        }
                    }

                    client_response.message = "drop";
                    json_send = JsonSerializer.Serialize(client_response);
                    bytes_send = Encoding.UTF8.GetBytes(json_send);
                    connection.Send(bytes_send);

                    // Release the socket.
                    connection.Shutdown(SocketShutdown.Both);

                    string args = "-b ";
                    args += blend_file;
                    args += " -o ";
                    args += "//frame_#### ";
                    args += "-F ";
                    args += master_response.file_format;
                    args += " -s ";
                    args += master_response.first_frame;
                    args += " -e ";
                    args += master_response.first_frame;
                    args += " -a";
                    if (master_response.render_engine == "CYCLES")
                    {
                        args += " --cycles-device ";
                        args += SETTINGS.render_device;
                    }

                    // Use Blender to obtain informations about the project
                    Process process = new Process();
                    // Set Blender as executable
                    process.StartInfo.FileName = SETTINGS.blender_executable;
                    // Use the command string as args
                    process.StartInfo.Arguments = args;
                    process.StartInfo.CreateNoWindow = true;
                    // Redirect output to log Blenders output
                    process.StartInfo.RedirectStandardOutput = true;
                    process.Start();
                    // Print and log the output
                    string cmd_output = "";
                    while (!process.HasExited)
                    {
                        cmd_output += process.StandardOutput.ReadToEnd();
                        Console.WriteLine(cmd_output);
                    }

                    client_response = new Client_Response();
                    client_response.message = "output";
                    client_response.files = new List<string>();
                    client_response.faulty = new List<bool>();
                    client_response.frames = new List<int>();

                    List<string> paths = new List<string>();

                    for (int frame = master_response.first_frame; frame <= master_response.last_frame; frame++)
                    {
                        client_response.frames.Add(frame);

                        string file_name = frame.ToString().PadLeft(4, '0') + "." + master_response.file_format;
                        client_response.files.Add(file_name);
                        string path = Path.Join(PROJECT_DIRECTORY, file_name);
                        paths.Add(path);

                        client_response.faulty.Add(!File.Exists(path));
                    }

                    string zip_name = master_response.first_frame + "_" + master_response.last_frame + ".zip";
                    string zip_file = Path.Join(PROJECT_DIRECTORY, zip_name);

                    if (master_response.use_zip)
                    {
                        using (ZipArchive archive = ZipFile.Open(zip_file, ZipArchiveMode.Create))
                        {
                            foreach (string path in paths)
                            {
                                archive.CreateEntryFromFile(path, Path.GetFileName(path));
                            }
                        }
                    }

                    if (master_response.use_ftp)
                    {
                        //upload to ftp server
                    }

                    while (true)
                    {
                        try
                        {
                            connection.Connect(remote_end_point);

                            json_send = JsonSerializer.Serialize(client_response);
                            bytes_send = Encoding.UTF8.GetBytes(json_send);
                            connection.Send(bytes_send);

                            if (client_response.faulty.Contains(false) && !master_response.use_ftp)
                            {
                                buffer = new byte[1024];
                                connection.Receive(buffer);

                                if (master_response.use_zip)
                                {
                                    

                                    connection.SendFile(zip_file);
                                }

                                else
                                {
                                    foreach (string path in paths)
                                    {
                                        connection.SendFile(path);
                                    }
                                }
                            }
                        }

                        catch (Exception e)
                        {

                        }
                        
                    }
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                    Thread.Sleep(2500);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Thread.Sleep(5000);
            }
        }
        
    }

    #region Menu_Display
    // Open a semi-graphical menu allowing easy user input
    public static string Menu(List<string> options, List<string> headlines)
    {
        // Hide cursor
        int selected = 0;
        Console.CursorVisible = false;

        // Create and fill list with given options
        List<Menu_Item> items = new List<Menu_Item>();
        foreach (string option in options)
        {
            items.Add(new Menu_Item(option));
        }// Create empty settings object
        Settings new_settings = new Settings();
        // Make the 1st option the default
        items[0].selected = true;

        // Infinite loop
        while (true)
        {
            // Clear everything and show the top bar
            Console.Clear();
            Show_Top_Bar();

            // Show an additional promt if given
            if (headlines.Count != 0)
            {
                // Print every line
                foreach (string headline in headlines)
                {
                    Console.WriteLine(headline);
                }
                //Console.WriteLine("#--------------------------------------------------------------#");
                //Console.WriteLine("");
            }

            // GO through all items and color them depending on the selection
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

            // Wait for user input
            ConsoleKeyInfo keyInfo = Console.ReadKey();
            ConsoleKey key = keyInfo.Key;

            // If possible go up in list
            if (key == ConsoleKey.UpArrow && selected != 0)
            {
                items[selected].selected = false;

                selected--;

                items[selected].selected = true;
            }

            // If possible go down in list
            else if (key == ConsoleKey.DownArrow && selected != (items.Count() - 1))
            {
                items[selected].selected = false;

                selected++;

                items[selected].selected = true;
            }

            // Return the current selection and break out of the loop
            // Show the cursor
            else if (key == ConsoleKey.Enter)
            {
                Console.Clear();
                Console.CursorVisible = true;
                return items[selected].text;
            }
        }
    }

    // Print the top bar
    public static void Show_Top_Bar()
    {
        Console.WriteLine("Pidgeon Render Farm");
        Console.WriteLine("Join the Discord server for support - https://discord.gg/cnFdGQP");
        Console.WriteLine("");
        Console.WriteLine("#--------------------------------------------------------------#");
        Console.WriteLine("");
    }
    #endregion

    #region Setup
    public static void First_Time_Setup()
    {
        // Create empty settings object
        Settings new_settings = new Settings();
        // Removes the need to type "new List<string> { "Yes", "No" }" every time
        List<string> basic_bool = new List<string> { "Yes", "No" };
        // user_input string
        string user_input;

        // Write version - for updating in the future
        new_settings.version = VERSION;

        // Enable logging
        // Use Menu() to grab user input
        new_settings.enable_logging = Parse_Bool(Menu(basic_bool, new List<string> { "Enable logging? (It is recommended to turn this on. To see whats included please refer to the documentation!)" }));

        // Add Master
        // Let the user input a valid IP adress with Port
        // E.g. "127.0.0.1:8080", "127.0.0.1:8081"
        Show_Top_Bar();
        Console.WriteLine("If you want to add a Master write it like this 'IPv4:Port', e.g. '127.0.0.1:8080'");
        while (true)
        {
            Console.WriteLine("Would you like to add another Master? (leave empty for no)");
            user_input = Console.ReadLine();

            if (user_input == "" && new_settings.masters.Count >= 0)
            {
                break;
            }

            else if (!Check_Split_IP_Port(user_input))
            {
                Console.WriteLine("Please input a valid combination of a IPv4 and a port");
                Console.WriteLine("If you are having trouble you can contact us on Discord at any time!");
            }

            else if (Check_Split_IP_Port(user_input))
            {
                string[] split = user_input.Split(':');
                new_settings.masters.Add(new Master(split[0], int.Parse(split[1])));
            }
        }
        Console.Clear();

        // Blender Executable
        // Check if the file exsists
        Show_Top_Bar();
        Console.WriteLine("Where is your blender executable stored? (It's recommended not to use blender-launcher)");
        user_input = Console.ReadLine().Replace("\"", "");
        while (!File.Exists(user_input))
        {
            Console.WriteLine("Please input the path to your blender executable");
            user_input = Console.ReadLine().Replace("\"", "");
        }
        new_settings.blender_executable = user_input;

        // Keep output
        // Use Menu() to grab user input
        new_settings.render_device = Menu(new List<string> { "CPU", "CUDA", "OPTIX", "HIP", "ONEAPI", "METAL", "OPENCL" },
                                          new List<string> { "What device/API to use for rendering? (Be sure your device and Blender version supports yor selection!)" });

        // Add CPU
        if (new_settings.render_device != "CPU")
        {
            // Use Menu() to grab user input
            if (Parse_Bool(Menu(basic_bool, new List<string> { "Enable Hybrid Cycles-rendering? (For some it cuts the render time, for some it increases it)" })))
            {
                new_settings.render_device = new_settings.render_device + "+CPU";
            }
        }

        // CPU Thread limit
        // Use Menu() to grab user input
        List<string> tmp = new List<string>();
        for (int thread = 0; thread < Environment.ProcessorCount; thread++)
        {
            tmp.Add((thread + 1).ToString());
        }
        tmp.Reverse();
        new_settings.limit_cpu_threads = int.Parse(Menu(tmp, new List<string> { "How many CPU threads to use? (used for compositing and rendering, if enabled; if you can't see all of your aviable threads, please contact us on Discord)" }));

        // RAM usage limit
        // Let the user input a valid number
        // If emtpy, then use use no limit
        Show_Top_Bar();
        Console.WriteLine("If you would like to limit the amount of RAM used for projects type a number here (in MegaByte/MB; type 0/leave empty for no limit):");
        user_input = Console.ReadLine();
        while (!int.TryParse(user_input, out _))
        {
            if (user_input == "")
            {
                user_input = "0";
                break;
            }

            Console.WriteLine("Please input a whole number");
            user_input = Console.ReadLine();
        }
        new_settings.limit_ram_use = Math.Abs(int.Parse(user_input));
        Console.Clear();

        // Time limit per frame
        // Let the user input a valid number
        // If emtpy, then use use no limit
        Show_Top_Bar();
        Console.WriteLine("If you would like to limit the amount of time consumed per frame type a number here (in seconds; type 0.0/leave empty for no limit):");
        user_input = Console.ReadLine();
        while (!float.TryParse(user_input, out _))
        {
            if (user_input == "")
            {
                user_input = "0.0";
                break;
            }

            Console.WriteLine("Please input a decimal number");
            user_input = Console.ReadLine();
        }
        new_settings.limit_time_frame = MathF.Abs(float.Parse(user_input));
        Console.Clear();

        // Select allowed render engines
        (new_settings.allowed_engines, new_settings.blender_version) = Pick_Render_Engines(new_settings.blender_executable, new_settings.enable_logging);

        // Keep input
        // Use Menu() to grab user input
        new_settings.keep_input = Parse_Bool(Menu(basic_bool, new List<string> { "Keep the files received from the Master?" }));

        // Keep output
        // Use Menu() to grab user input
        new_settings.keep_output = Parse_Bool(Menu(basic_bool, new List<string> { "Keep the files rendered on this device?" }));

        // Data collection
        // Use Menu() to grab user input
        new_settings.collect_data = Parse_Bool(Menu(basic_bool, new List<string> { "Allow us to collect data? (We have no acess to it, even if you enter yes!) " }));

        // Save the settings
        Save_Settings(new_settings);
    }
    public static (List<string>,string) Pick_Render_Engines(string blender_executable, bool enable_logging)
    {
        // Tell the user why it is taking so long
        Show_Top_Bar();
        Console.WriteLine("Please wait while importing installed render engines from Blender...");

        // Build arguments string: hidden, execute Get_Engines.py
        string args = "-b ";
        args += " -P ";
        args += "Get_Engines.py";
        args += " -- ";
        args += SCRIPT_DIRECTORY;
        // Use Blender to obtain informations about the project
        Process process = new Process();
        // Set Blender as executable
        process.StartInfo.FileName = blender_executable;
        // Use the command string as args
        process.StartInfo.Arguments = args;
        process.StartInfo.CreateNoWindow = true;
        // Redirect output to log Blenders output
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();
        // Print and log the output
        string cmd_output = "";
        while (!process.HasExited)
        {
            cmd_output += process.StandardOutput.ReadToEnd();
            Console.WriteLine(cmd_output);
        }
        Write_Log(cmd_output, enable_logging);
        // Split the content of the file into a List
        List<string> engines = new List<string>();
        List<string> lines = (List<string>)File.ReadLines(Path.Join(SCRIPT_DIRECTORY, "engines.json"));
        string version = lines[0];
        lines.Remove(version);
        foreach (string line in lines)
        {
            engines.Add(line);
        }

        // Create list with all options and hand it to Menu()
        List<string> items = new List<string>
        {
            "Allow all installed engines",
            "Select allowed from installed engines"
        };
        // Show the Menu and grab the selection
        string selection = Menu(items, new List<string> { "" });
        // Compare selection with options and execute function
        if (selection == items[0])
        {
            // Return all installed engines
            return (engines, version);
        }

        else if (selection == items[1])
        {
            List<string> picked_engines = new List<string>();
            engines.Add("That is it, I don't want to allow more engines");

            // let the user pick one by one
            while (engines.Count > 1)
            {
                selection = Menu(engines, new List<string> { "Choose one by one which render engines to allow." });

                if (selection != "That is it, I don't want to allow more engines")
                {
                    engines.Remove(selection);
                    picked_engines.Add(selection);
                }
            }

            return (picked_engines, version);
        }

        return (null, version);
    }

    // Save settings based on a new set
    public static void Save_Settings(Settings new_settings)
    {
        // Update global settings object
        SETTINGS = new_settings;

        // Convert object to json
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(SETTINGS, options);

        // Write to file
        File.WriteAllText(SETTINGS_FILE, jsonString);
    }

    // Load the settings
    public static void Load_Settings(string custom_file = "")
    {
        if (custom_file == "")
        {
            custom_file = SETTINGS_FILE;
        }

        // Check if settings exsist, else run setup
        if (!File.Exists(custom_file))
        {
            First_Time_Setup();
            return;
        }

        // Load string from file and convert it to object
        // Update global settings object
        string json_string = File.ReadAllText(custom_file);
        SETTINGS = JsonSerializer.Deserialize<Settings>(json_string);
    }
    #endregion

    #region Data Handeling
    // Write text to log file
    public static void Write_Log(string content)
    {
        if (SETTINGS.enable_logging)
        {
            // Make sure file exsists
            if (!File.Exists(LOGS_FILE))
            {
                File.Create(LOGS_FILE).Close();
            }

            // Append line break
            content += Environment.NewLine;

            // Append new line to file
            File.AppendAllText(LOGS_FILE, content);
        }
    }
    // Write text to log file with logging overwrite
    public static void Write_Log(string content, bool overwrite)
    {
        if (overwrite)
        {
            // Make sure file exsists
            if (!File.Exists(LOGS_FILE))
            {
                File.Create(LOGS_FILE).Close();
            }

            // Append line break
            content += Environment.NewLine;

            // Append new line to file
            File.AppendAllText(LOGS_FILE, content);
        }
    }

    // Collect system data
    public static void Collect_Data()
    {
        // Create empty PRF_Data object
        PRF_Data new_data = new PRF_Data();

        // ONLY proceed if the user agrees
        if (SETTINGS.collect_data)
        {
            // Gather informations and add to data object
            new_data.version = VERSION;
            new_data.os = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        }

        // Save the obtained data
        Save_Data(new_data);
    }

    // Save data object
    public static void Save_Data(PRF_Data new_data)
    {
        // Update global PRF_DATA object
        PRF_DATA = new_data;

        // Convert object to json
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json_string = JsonSerializer.Serialize(PRF_DATA, options);

        // Write json to file
        File.WriteAllText(DATA_FILE, json_string);
    }

    // Load PRF_DATA
    public static void Load_Data(PRF_Data new_data)
    {
        // Prevent read errors
        if (!File.Exists(DATA_FILE))
        {
            Collect_Data();
            return;
        }

        // Read json from file
        // Convert json to object
        // Update global PRF_DATA object
        string json_string = File.ReadAllText(DATA_FILE);
        PRF_DATA = JsonSerializer.Deserialize<PRF_Data>(json_string);
    }
    #endregion

    #region Helpers
    // Check if string is a valid port
    public static bool Is_Port(string port)
    {
        // Check if string is number
        if (!int.TryParse(port, out _))
        {
            return false;
        }

        // Check if number is between 1 and 65535
        return (Math.Abs(int.Parse(port)) >= 1 && Math.Abs(int.Parse(port)) <= 65535);
    }

    // Convert string to bool, accepts default
    public static dynamic Parse_Bool(string value, bool def = true)
    {
        // If human is not sure, return default value/let developer decide
        if (value == "")
        {
            return def;
        }

        // If it is a human yes, return computer true
        else if (new List<string>() { "true", "yes", "y", "1" }.Any(s => s.Contains(value.ToLower())))
        {
            return true;
        }

        // If it is a human no, return computer false
        else if (new List<string>() { "false", "no", "n", "0" }.Any(s => s.Contains(value.ToLower())))
        {
            return false;
        }

        // if none applies, return error
        return null;
    }

    // Convert string to List of strings, requires default
    public static List<string> Parse_List(string value, List<string> def)
    {
        // If human is not sure, return default value/let developer decide
        if (value == "")
        {
            return def;
        }

        // If human used "," to seperate
        else if (value.Split(",").Length > 0)
        {
            return value.Split(",").ToList();
        }

        // If human used ", " to seperate
        else if (value.Split(", ").Length > 0)
        {
            return value.Split(", ").ToList();
        }

        // if none applies, return error
        return null;
    }

    // Check if string is a IPv4
    // Return bool
    // Credit: https://stackoverflow.com/users/961113/habib
    public static bool Validate_IPv4(string ipString)
    {
        if (String.IsNullOrWhiteSpace(ipString))
        {
            return false;
        }

        string[] splitValues = ipString.Split('.');
        if (splitValues.Length != 4)
        {
            return false;
        }

        byte tempForParsing;

        return splitValues.All(r => byte.TryParse(r, out tempForParsing));
    }

    public static bool Check_Split_IP_Port(string value)
    {
        if (value.Split(':').Length != 2)
        {
            return false;
        }

        if (!Validate_IPv4(value.Split(":")[0]))
        {
            return false;
        }

        if (!Is_Port(value.Split(":")[1]))
        {
            return false;
        }

        return true;
    }
    #endregion
}

#region Objects
// Settings object class
public class Settings
{
    public float version { get; set; } = 0.0f;
    public List<Master> masters { get; set; } = new List<Master>();
    public string blender_executable { get; set; }
    public string blender_version { get; set; }
    public string render_device { get; set; }
    public int limit_cpu_threads { get; set; } = 0;
    public int limit_ram_use { get; set; } = 0;
    public float limit_time_frame { get; set; } = 0;
    public List<string> allowed_engines { get; set; }
    public bool keep_input { get; set; }
    public bool keep_output { get; set; }
    public bool collect_data { get; set; }
    public bool enable_logging { get; set; }
}

public class Master
{
    public string ip { get; set; } = "127.0.0.1";
    public int port { get; set; } = 8080;

    public Master(string new_ip, int new_port)
    {
        ip = new_ip;
        port = new_port;
    }
    public Master()
    {
        
    }
}

public class Client_Response
{
    public string message { get; set; }
    public string blender_version { get; set; } = null;
    public List<string> allowed_engines { get; set; }
    public int limit_ram_use { get; set; }
    public float limit_file_size { get; set; }
    public float limit_time_frame { get; set; }
    public List<bool> faulty { get; set; }
    public List<int> frames { get; set; }
    public List<string> files { get; set; }
}

public class Master_Response
{
    public string message { get; set; }
    public bool use_ftp { get; set; }
    public bool use_zip { get; set; }
    public string id { get; set; }
    public float file_size { get; set; }
    public string render_engine { get; set; }
    public string file_format { get; set; }
    public int first_frame { get; set; }
    public int last_frame { get; set; }

}

// PRF_Data object class
public class PRF_Data
{
    public float version { get; set; } = 0.0f;
    public string os { get; set; } = "No data";
    public List<string> cpus { get; set; } = new List<string> { "No data" };
    public List<string> gpus { get; set; } = new List<string> { "No data" };
    public int ram { get; set; } = 0;
}

// Menu_Item object class
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
