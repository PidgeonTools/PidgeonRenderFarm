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
    public static string VERSION = "0.1.0-beta";
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
        LOGS_FILE = Path.Join(LOGS_DIRECTORY, ("client_" + start_time.ToString("HH_mm_ss") + ".txt"));
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
            "Donate",
            "Exit"
        };

        while (true)
        {
            // Show the Menu and grab the selection
            string selection = Menu(items, new List<string> { });

            // Compare selection with options and execute function
            if (selection == items[0])
            {
                // Start the worker
                Worker();
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
            }

            else if (selection == items[3])
            {
                // Open Discord invite in browser
                Process.Start(new ProcessStartInfo("https://discord.gg/cnFdGQP") { UseShellExecute = true });
            }

            else if (selection == items[4])
            {
                // Open Paypal in browser
                Process.Start(new ProcessStartInfo("https://www.paypal.me/kevinlorengel") { UseShellExecute = true });
            }

            else
            {
                // Exit PRF
                Environment.Exit(1);
            }
        }
    }

    // Main thread
    public static void Worker()
    {
        try
        {
            IPAddress ip_address = Get_IPv4();
            Show_Top_Bar(new List<string> { "Client IP address: " + ip_address.ToString() });
        }
        catch
        {
            Show_Top_Bar();
        }

        // Repeat forever (is gonna change)
        while (true)
        {
            try
            {
                // Parse master ip
                IPAddress ip_address = IPAddress.Parse(SETTINGS.masters[0].ip);
                // Add master as end point using ip and port
                IPEndPoint remote_end_point = new IPEndPoint(ip_address, SETTINGS.masters[0].port);
                // Socket for connection -> TCP
                Socket connection = new Socket(ip_address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    string log = "";

                    DateTime connecting_time = DateTime.Now;
                    Console.WriteLine("Trying to connect to: " + SETTINGS.masters[0].ip + " @ " + connecting_time.ToString("HH:mm:ss"));
                    Write_Log("Trying to connect to: " + SETTINGS.masters[0].ip + " @ " + connecting_time.ToString("HH:mm:ss"));

                    // Connect to Master
                    connection.Connect(remote_end_point);

                    // Log the time and ip of the Master connection
                    DateTime connection_time = DateTime.Now;
                    Console.WriteLine("Connected to: " + connection.RemoteEndPoint.ToString() + " @ " + connection_time.ToString("HH:mm:ss"));
                    Write_Log("Connected to: " + connection.RemoteEndPoint.ToString() + " @ " + connection_time.ToString("HH:mm:ss"));

                    // Prepare request
                    Client_Response client_response = new Client_Response();

                    // Request a job
                    client_response.message = "new";
                    client_response.blender_version = SETTINGS.blender_version;
                    client_response.allowed_engines = SETTINGS.allowed_engines;
                    client_response.limit_ram_use = SETTINGS.limit_ram_use;
                    client_response.limit_time_frame = SETTINGS.limit_ram_use;
                    // Convert the object to string
                    string json_send = JsonSerializer.Serialize(client_response);
                    // Convert string to bytes
                    byte[] bytes_send = Encoding.UTF8.GetBytes(json_send);
                    // Send bytes to Master
                    connection.Send(bytes_send);

                    // Receive message from Master
                    byte[] buffer = new byte[1024];
                    int received = connection.Receive(buffer);
                    // Convert bytes to string
                    string json_receive = Encoding.UTF8.GetString(buffer, 0, received);
                    // Convert string to an object
                    Master_Response master_response = JsonSerializer.Deserialize<Master_Response>(json_receive);

                    if (master_response.message == "NAN")
                    {
                        Console.WriteLine(connection.RemoteEndPoint.ToString() + ": no new job");
                        Write_Log(connection.RemoteEndPoint.ToString() + ": no new job");
                        connection.Shutdown(SocketShutdown.Both);
                        connection.Close();
                        Thread.Sleep(20000);
                        continue;
                    }

                    else
                    {
                        log = string.Format("{0}; Project: {1}; Frame: {2} - {3}",
                                            connection.RemoteEndPoint.ToString(),
                                            master_response.id,
                                            master_response.first_frame,
                                            master_response.last_frame);
                        Console.WriteLine(log);
                        Write_Log(log);
                    }

                    // Prepare request
                    client_response = new Client_Response();

                    PROJECT_DIRECTORY = Path.Join(SCRIPT_DIRECTORY, master_response.id.ToString());
                    //Console.WriteLine(PROJECT_DIRECTORY);

                    // Check if the directory exsists
                    if (!Directory.Exists(PROJECT_DIRECTORY))
                    {
                        // If not -> create it
                        Directory.CreateDirectory(PROJECT_DIRECTORY);
                    }

                    // Generate .blend file name
                    string blend_file = Path.Join(PROJECT_DIRECTORY, (master_response.id + ".blend"));
                    // grab it's size
                    long file_size = 0;
                    try
                    {
                        file_size = new FileInfo(blend_file).Length;
                    }
                    catch { }

                    // Compare if out file is the same as the Master's
                    if (!File.Exists(blend_file) || file_size != master_response.file_size)
                    {
                        // If it's not the same
                        // Tell the Master we still need the file
                        client_response.message = "needed";
                        // Convert the object to string
                        json_send = JsonSerializer.Serialize(client_response);
                        // Convert string to bytes
                        bytes_send = Encoding.UTF8.GetBytes(json_send);
                        // Send bytes to Master
                        connection.Send(bytes_send);

                        // Download the file to path
                        using (FileStream file_stream = File.Create(blend_file))
                        {
                            new NetworkStream(connection).CopyTo(file_stream);
                        }

                        Console.WriteLine("Downloaded " + blend_file);
                        Write_Log("Downloaded " + blend_file);
                    }

                    else
                    {
                        // If it's the same just send a message to drop
                        client_response.message = "drop";
                        // Convert the object to string
                        json_send = JsonSerializer.Serialize(client_response);
                        // Convert string to bytes
                        bytes_send = Encoding.UTF8.GetBytes(json_send);
                        // Send bytes to Master
                        connection.Send(bytes_send);

                        Console.WriteLine(blend_file + " already exsists");
                        Write_Log(blend_file + " already exsists");
                    }

                    // Cut the connection
                    connection.Shutdown(SocketShutdown.Both);
                    connection.Close();

                    if (master_response.use_sid_temporal)
                    {
                        Render_SID_Temporal(blend_file
                                           , master_response.first_frame
                                           , master_response.last_frame
                                           , master_response.render_engine);
                    }
                    else
                    {
                        Render(blend_file,
                               master_response.first_frame,
                               master_response.last_frame,
                               master_response.render_engine,
                               master_response.file_format);
                    }

                    log = string.Format("Frame {0} - {1} rendered!", master_response.first_frame, master_response.last_frame);
                    Console.WriteLine(log);
                    Write_Log(log);

                    // Prepare new response
                    client_response = new Client_Response();
                    client_response.message = "output";
                    client_response.files = new List<string>();
                    client_response.faulty = new List<bool>();
                    client_response.frames = new List<int>();

                    // List for paths -> send files
                    List<string> paths = new List<string>();

                    string base_directory = PROJECT_DIRECTORY;

                    if (master_response.use_sid_temporal)
                    {
                        base_directory = Path.Join(base_directory, "noisy");
                        string[] dirs = Directory.GetDirectories(base_directory);

                        int highest = 0;

                        foreach (string dir in dirs)
                        {
                            int dir_number = int.Parse(dir.Split(Path.DirectorySeparatorChar).Last());

                            if (dir_number >= highest)
                            {
                                highest = dir_number;
                            }
                        }

                        base_directory = Path.Join(base_directory, highest.ToString());
                    }

                    // itterate through all rendered frames
                    for (int frame = master_response.first_frame; frame <= master_response.last_frame; frame += master_response.frame_step)
                    {
                        // Add frame to list
                        client_response.frames.Add(frame);

                        string file_name = "";
                        string path = "";

                        if (master_response.use_sid_temporal)
                        {
                            // Generate the file name
                            file_name = frame.ToString().PadLeft(6, '0') + ".exr";
                            // Add file to list
                            client_response.files.Add(file_name);
                            // Get it's path
                            path = Path.Join(base_directory, file_name);
                        }
                        else
                        {
                            // Generate the file name
                            file_name = "frame_" + frame.ToString().PadLeft(6, '0') + "." + master_response.file_format;
                            // Get it's path
                            path = Path.Join(PROJECT_DIRECTORY, file_name);
                            // Add file to list
                            client_response.files.Add(file_name);
                            // Add path to list
                            paths.Add(path);
                        }

                        // Set faulty value based on exsistance
                        client_response.faulty.Add(!File.Exists(path));
                    }

                    // Prepare zip file name and path
                    string zip_name = master_response.first_frame + "_" + master_response.last_frame + ".zip";
                    string zip_file = Path.Join(PROJECT_DIRECTORY, zip_name);

                    // Add frames to ZIP
                    using (ZipArchive archive = ZipFile.Open(zip_file, ZipArchiveMode.Create))
                    {
                        if (!master_response.use_sid_temporal)
                        {
                            foreach (string path in paths)
                            {
                                archive.CreateEntryFromFile(path, Path.GetFileName(path));
                            }
                        }
                        else
                        {
                            base_directory = PROJECT_DIRECTORY;
                            base_directory = Path.Join(base_directory, "noisy");
                            archive.CreateEntry("noisy");

                            foreach (string folder in Directory.GetDirectories(base_directory))
                            {
                                string only_folder = folder.Split(Path.DirectorySeparatorChar).Last();
                                only_folder = Path.Join("noisy", only_folder);

                                archive.CreateEntry(only_folder);

                                foreach (string file in Directory.GetFiles(folder))
                                {
                                    string only_file = file.Split(Path.DirectorySeparatorChar).Last();
                                    archive.CreateEntryFromFile(file, Path.Join(only_folder, only_file));
                                }
                            }
                        }
                    }

                    if (master_response.use_ftp)
                    {
                        //upload to ftp server
                    }

                    log = string.Format("Frame {0} - {1} uploaded!", master_response.first_frame, master_response.last_frame);
                    Console.WriteLine(log);
                    Write_Log(log);

                    // Make sure the Master receives them at any cost
                    while (true)
                    {
                        try
                        {
                            connection = new Socket(ip_address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                            // Try to connect
                            connection.Connect(remote_end_point);

                            // Convert the object to string
                            json_send = JsonSerializer.Serialize(client_response);
                            // Convert string to bytes
                            bytes_send = Encoding.UTF8.GetBytes(json_send);
                            // Send bytes to client
                            connection.Send(bytes_send);

                            // If there are any good frames and in non FTP mode
                            if (client_response.faulty.Contains(false) && !master_response.use_ftp)
                            {
                                // Synchronize with Master
                                buffer = new byte[1024];
                                connection.Receive(buffer);

                                // Send the ZIP file
                                connection.SendFile(zip_file);
                                break;
                            }

                            break;
                        }
                        catch (Exception e)
                        {
                            // Log errors
                            Write_Log(e.ToString());
                        }
                        
                    }

                    // Cut the connection
                    connection.Shutdown(SocketShutdown.Both);
                    connection.Close();
                }
                catch (Exception e)
                {
                    if (e is SocketException)
                    {
                        Console.WriteLine("Can't connect to Master");
                    }
                    else
                    {
                        // Log errors
                        Console.WriteLine("Unexpected exception : {0}", e.ToString());
                    }

                    Write_Log(e.ToString());

                    Thread.Sleep(10000);
                }
            }
            catch (Exception e)
            {
                // Log errors
                Console.WriteLine(e.ToString());
                Write_Log(e.ToString());
                // Wait for 30 seconds
                Thread.Sleep(30000);
            }
        }
        
    }

    public static void Render_SID_Temporal(string blend_file, int first_frame, int last_frame, string render_engine)
    {
        // Prepare Blender arguments
        string args = string.Format("-b \"{0}\" -t {1} -P SID_Temporal_Bridge.py -- {2} {3}",
                                    blend_file,
                                    SETTINGS.limit_cpu_threads,
                                    first_frame,
                                    last_frame);
        // Add render device for Cycles
        if (render_engine == "CYCLES")
        {
            args += " --cycles-device ";
            args += SETTINGS.render_device;
        }

        // Use Blender to render project
        Process process = new Process();
        // Set Blender as executable
        process.StartInfo.FileName = SETTINGS.blender_executable;
        // Use the command string as args
        process.StartInfo.Arguments = args;
        process.StartInfo.CreateNoWindow = true;
        // Redirect output to log Blenders output
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();

        Console.WriteLine("Rendering frame {0} - {1}...", first_frame, last_frame);

        // Log the output
        process.WaitForExit();
        string cmd_output = process.StandardOutput.ReadToEnd();
        Write_Log(cmd_output);
    }

    public static void Render(string blend_file, int first_frame, int last_frame, string render_engine, string file_format)
    {
        // Prepare Blender arguments
        string args = string.Format("-b \"{0}\" -o {1} -F {2} -s {3} -e {4} -t {5} -a",
                                    blend_file,
                                    Path.Join(PROJECT_DIRECTORY, "frame_######"),
                                    file_format,
                                    first_frame,
                                    last_frame,
                                    SETTINGS.limit_cpu_threads);
        // Add render device for Cycles
        if (render_engine == "CYCLES")
        {
            args += " --cycles-device ";
            args += SETTINGS.render_device;
        }

        // Use Blender to render project
        Process process = new Process();
        // Set Blender as executable
        process.StartInfo.FileName = SETTINGS.blender_executable;
        // Use the command string as args
        process.StartInfo.Arguments = args;
        process.StartInfo.CreateNoWindow = true;
        // Redirect output to log Blenders output
        //process.StartInfo.RedirectStandardOutput = true;
        process.Start();

        Console.WriteLine("Rendering frame {0} - {1}...", first_frame, last_frame);

        // Log the output
        process.WaitForExit();
        //string cmd_output = process.StandardOutput.ReadToEnd();
        //Write_Log(cmd_output);
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
    public static void Show_Top_Bar(List<string> addition = null)
    {
        Console.WriteLine("Pidgeon Render Farm - Client");
        Console.WriteLine("Join the Discord server for support - https://discord.gg/cnFdGQP");
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
        Console.WriteLine("If you want to add a Master write it like this 'IPv4:Port', e.g. '127.0.0.1:19186'");
        while (true)
        {
            Console.WriteLine("Would you like to add another Master? (leave empty for no)");
            user_input = Console.ReadLine();

            if (user_input == "" && new_settings.masters.Count > 0)
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
            user_input = user_input.Replace("'", "");
        }
        new_settings.blender_executable = user_input;

        // Keep output
        // Use Menu() to grab user input
        new_settings.render_device = Menu(new List<string> { "CPU", "CUDA", "OPTIX", "HIP", "ONEAPI", "METAL", "OPENCL" },
                                          new List<string> { "What device/API to use for rendering? (Be sure your device and Blender version supports your selection!)" });

        // Add CPU
        if (new_settings.render_device != "CPU")
        {
            // Use Menu() to grab user input
            if (Parse_Bool(Menu(basic_bool, new List<string> { "Enable Hybrid Cycles-rendering? (For some it cuts the render time, for some it increases it)" })))
            {
                new_settings.render_device = new_settings.render_device + "+CPU";
            }
        }
        else
        {
            new_settings.limit_cpu_threads = Environment.ProcessorCount + 1;
        }

        // Select allowed render engines
        (new_settings.allowed_engines, new_settings.blender_version) = Pick_Render_Engines(new_settings.blender_executable, new_settings.enable_logging);

        // Keep input
        // Use Menu() to grab user input
        //new_settings.keep_input = Parse_Bool(Menu(basic_bool, new List<string> { "Keep the files received from the Master?" }));

        // Keep output
        // Use Menu() to grab user input
        //new_settings.keep_output = Parse_Bool(Menu(basic_bool, new List<string> { "Keep the files rendered on this device?" }));

        // Data collection
        // Use Menu() to grab user input
        new_settings.collect_data = Parse_Bool(Menu(basic_bool, new List<string> { "Allow us to collect data? (it is only stored locally for debugging purposes)" }));

        bool advances_settings = Parse_Bool(Menu(basic_bool, new List<string> { "Would you like to edit the advanced settings?" }));

        if (advances_settings)
        {
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
            /*Show_Top_Bar();
            Console.WriteLine("If you would like to limit the amount of RAM used for projects type a number here (in GigaByte/GB; type 0/leave empty for no limit):");
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
            Console.Clear();*/

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
        }

        // Save the settings
        Save_Settings(new_settings);
    }
    public static (List<string>,string) Pick_Render_Engines(string blender_executable, bool enable_logging)
    {
        // Tell the user why it is taking so long
        Show_Top_Bar();
        Console.WriteLine("Please wait while importing installed render engines from Blender...");

        // Prepare Blender arguments
        string args = "-b -P Get_Engines.py";

        // Use Blender to obtain the render engines and Blender version
        Process process = new Process();
        // Set Blender as executable
        process.StartInfo.FileName = blender_executable;
        // Use the command string as args
        process.StartInfo.Arguments = args;
        process.StartInfo.CreateNoWindow = true;
        // Redirect output to log Blenders output
        //process.StartInfo.RedirectStandardOutput = true;
        process.Start();
        // Log the output
        process.WaitForExit();
        //string cmd_output = process.StandardOutput.ReadToEnd();
        //Write_Log(cmd_output, enable_logging);

        // Split the content of the file into a List
        List<string> engines = new List<string>();
        List<string> lines = File.ReadLines(Path.Join(SCRIPT_DIRECTORY, "engines.json")).ToList();
        string version = lines[0];
        lines.Remove(version);
        foreach (string line in lines)
        {
            engines.Add(line);
        }

        // Create list with all options and hand it to Menu()
        List<string> items = new List<string>
        {
            "Allow all engines (recommended)",
            "Allow all installed engines",
            "Select allowed from installed engines"
        };
        // Show the Menu and grab the selection
        string selection = Menu(items, new List<string> { "" });
        // Compare selection with options and execute function
        if (selection == items[0])
        {
            // Return all installed engines
            return (new List<string> { "other" }, version);
        }

        else if (selection == items[1])
        {
            // Return all installed engines
            return (engines, version);
        }

        else if (selection == items[2])
        {
            // If the user wants to pick manually
            List<string> picked_engines = new List<string>();
            engines.Add("That is it, I don't want to allow more engines");

            // let the user pick one by one
            while (engines.Count > 1)
            {
                // Give him a selection of engines
                selection = Menu(engines, new List<string> { "Choose one by one which render engines to allow." });

                // If he isn't done remove from engines remaining and add to picked
                if (selection != "That is it, I don't want to allow more engines")
                {
                    engines.Remove(selection);
                    picked_engines.Add(selection);
                }

                else if (selection == "That is it, I don't want to allow more engines" && picked_engines.Count >= 1)
                {
                    break;
                }
            }

            // Return user choice and Blender version
            return (picked_engines, version);
        }

        // Return null and Blender version
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
        // If empty file -> use global file
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
        // Only log stuff if the users allows it
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

    // Check if a given ip with port is valid
    public static bool Check_Split_IP_Port(string value)
    {
        // If split is not 2 parts -> invalid
        if (value.Split(':').Length != 2)
        {
            return false;
        }

        // If ip part is not IPv4 -> invalid
        if (!Validate_IPv4(value.Split(":")[0]))
        {
            return false;
        }

        // If port part is not port -> invalid
        if (!Is_Port(value.Split(":")[1]))
        {
            return false;
        }

        // If checks passed it is valid
        return true;
    }

    public static IPAddress Get_IPv4()
    {
        // Obtain the ip adresses of the current machine
        IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

        // Check each adress -> find out if it's local IPv4
        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip;
            }
        }

        // If not connected only use local IP
        return IPAddress.Parse("127.0.0.1");
    }
    #endregion
}

#region Objects
// Settings object class
public class Settings
{
    public string version { get; set; } = "0.0.0";
    public List<Master> masters { get; set; } = new List<Master>();
    public string blender_executable { get; set; }
    public string blender_version { get; set; }
    public string render_device { get; set; }
    public int limit_cpu_threads { get; set; } = 2;
    public int limit_ram_use { get; set; } = 0;
    public float limit_time_frame { get; set; } = 0;
    public List<string> allowed_engines { get; set; }
    public bool keep_input { get; set; }
    public bool keep_output { get; set; }
    public bool collect_data { get; set; }
    public bool enable_logging { get; set; }
}

// Master class -> saved connection
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

// Communication to Master
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

// Communication from Master
public class Master_Response
{
    public string message { get; set; }
    public bool use_ftp { get; set; }
    public bool use_sid_temporal { get; set; }
    public string id { get; set; }
    public float file_size { get; set; }
    public string render_engine { get; set; }
    public string file_format { get; set; }
    public int first_frame { get; set; }
    public int last_frame { get; set; }
    public int frame_step { get; set; } = 1;

}

// PRF_Data object class
public class PRF_Data
{
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
