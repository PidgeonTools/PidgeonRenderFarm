using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

class Master
{
    #region Global Variables
    // Initialize global variables
    // Initialize global File names, directories
    public static string SCRIPT_DIRECTORY = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
    public static string LOGS_DIRECTORY = "";
    public static string LOGS_FILE = "";
    public static string PROJECT_DIRECTORY = "";
    public static string PROJECT_EXTENSION = ".prfp";
    public static string SETTINGS_FILE = "";
    public static string DATA_FILE = "";
    public static string BACKUP_FILE = "";

    // Create global objects and variables
    public static string VERSION = "0.1.0-beta";
    public static string ip_address_string = "127.0.0.1";
    public static Settings SETTINGS;
    public static PRF_Data PRF_DATA;
    public static Project PROJECT;
    public static List<int> frames_left = new List<int>();
    private static readonly object frame_lock = new object();
    private static readonly object done_lock = new object();
    private static readonly object print_lock = new object();
    public static SRF srf;
    public static Progress_Bar progress_bar;
    public static bool use_srf = false;
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
        LOGS_FILE = Path.Join(LOGS_DIRECTORY, ("master_" + start_time.ToString("HH_mm_ss") + ".txt"));
        SETTINGS_FILE = Path.Join(SCRIPT_DIRECTORY, "master_settings.json");
        DATA_FILE = Path.Join(SCRIPT_DIRECTORY, "master_data.json");
        BACKUP_FILE = Path.Join(SCRIPT_DIRECTORY, "PRF_Console.txt");

        // Main loop start
        Load_Settings();
        Collect_Data();

        // If provided with arguments -> load them as project
        if (args.Length != 0)
        {
            try
            {
                Write_Log(args[0]);
                Load_Project_From_String(args[0]);
            }
            catch (Exception e)
            {
                // Log errors
                Write_Log(e.ToString());
            }
        }

        // If it fails, just continue to the main menu
        Main_Menu();
    }
    
    public static void Main_Menu()
    {
        // Create list with all options and hand it to Menu()
        List<string> items = new List<string>
        {
            "New project",
            "Load project from disk",
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
                // Create a new project
                Project_Setup();
            }

            else if (selection == items[1])
            {
                // PRFP file
                // Let user input a valid PRF project file
                Show_Top_Bar();
                Console.WriteLine("Where is your PidgeonRenderFarm project stored?");
                string user_input = Console.ReadLine().Replace("\"", "");
                while (!File.Exists(user_input) && !user_input.EndsWith(".prfp"))
                {
                    Console.WriteLine("Please input the path to your .prfp");
                    user_input = Console.ReadLine().Replace("\"", "");
                }

                // Load the project from the given file
                Load_Project(user_input);
            }

            else if (selection == items[2])
            {
                // Run the first time setup and come back here
                First_Time_Setup();
            }

            else if (selection == items[3])
            {
                // Open documentation on Github.com
                Process.Start(new ProcessStartInfo("https://github.com/PidgeonTools/PidgeonRenderFarm") { UseShellExecute = true });
            }

            else if (selection == items[4])
            {
                // Open Discord invite in browser
                // https://discord.gg/cnFdGQP
                Process.Start(new ProcessStartInfo("https://discord.gg/pidgeon-tools-blender-3d-697931587387392010") { UseShellExecute = true });
            }

            else if (selection == items[5])
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

    public static void Render_Project()
    {
        Console.Clear();
        progress_bar = new Progress_Bar(Get_Frames_Done().Count, PROJECT.frames_total, "Rendering: ");

        // Get the local IPv4 of device
        IPAddress ip_address = Get_IPv4();
        ip_address_string = ip_address.ToString();
        Show_Top_Bar(new List<string> {"Master IP address: " + ip_address_string
                                       , "Master Port: " + SETTINGS.port.ToString()});

        List<Thread> threads = new List<Thread>();

        lock (print_lock)
        {
            Console.WriteLine(progress_bar.Get_Progress_Bar());
        }

        // Create a end point with given IP and port
        IPEndPoint local_end_point = new IPEndPoint(ip_address, SETTINGS.port);

        try
        {
            // Socket for clients initial connection (TCP)
            Socket listener = new(ip_address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            // Bind the end point to the new socket
            listener.Bind(local_end_point);
            // Set a maximum of 10 connected clients
            listener.Listen(10);

            Console.WriteLine("Waiting for Clients...");

            // While there are unrendered frames await clients
            while (Get_Frames_Done().Count < PROJECT.frames_total)
            {
                try
                {
                    // Accept a new connection
                    Socket handler = listener.Accept();

                    // Log the time and ip of the Client connection
                    DateTime connection_time = DateTime.Now;
                    lock (print_lock)
                    {
                        Console.WriteLine("New connection: " + handler.RemoteEndPoint.ToString() + " @ " + connection_time.ToString("HH:mm:ss"));
                        Write_Log("New connection: " + handler.RemoteEndPoint.ToString() + " @ " + connection_time.ToString("HH:mm:ss"));
                    }

                    Thread thread = new Thread(() => Client_Handler(handler, handler.RemoteEndPoint.ToString()));
                    threads.Add(thread);
                    thread.Start();

                    // Continue in the Client_Handler()
                    //Client_Handler(handler);
                }
                catch (Exception e)
                {
                    // Log errors
                    lock (print_lock)
                    {
                        Console.WriteLine("An error occurred, trying to continue anyways... (see the log files for more details)");
                        Write_Log(e.ToString());
                    }
                }
            }

            foreach (Thread thread in threads)
            {
                try
                {
                    thread.Abort();
                }
                catch { }
            }

            //listener.Shutdown(SocketShutdown.Both);
            listener.Close();
        }
        catch (Exception e)
        {
            // Log errors
            lock (print_lock)
            {
                Console.WriteLine(e.ToString());
                Write_Log(e.ToString());
            }
        }

        //Console.WriteLine("Rendering done! You can go back to the main menu by press any key");
        //Console.ReadKey();

        DateTime end_time = DateTime.Now;
        lock (print_lock)
        {
            Console.WriteLine("Rendering done! @" + end_time.ToString("HH:mm:ss"));
            Write_Log("Rendering done! @" + end_time.ToString("HH:mm:ss"));
        }

        if (PROJECT.use_sid_temporal)
        {
            // Prepare Blender arguments
            string args = string.Format("-b \"{0}\" -P SID_Temporal_Bridge.py",
                                 PROJECT.full_path_blend);

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
            // Print and log the output
            string cmd_output = "";
            while (!process.HasExited)
            {
                cmd_output += process.StandardOutput.ReadToEnd();
                Console.WriteLine(cmd_output);
            }
            Write_Log(cmd_output);

            end_time = DateTime.Now;
            lock (print_lock)
            {
                Console.WriteLine("Denoising done! @" + end_time.ToString("HH:mm:ss"));
                Write_Log("Denoising done! @" + end_time.ToString("HH:mm:ss"));
            }
        }

        if (!use_srf)
        {
            Console.WriteLine("You can go back to the main menu by pressing any key");
            Console.ReadKey();
        }
        else
        {
            Environment.Exit(1);
        }
    }

    public static void Client_Handler(Socket client, string client_ip)
    {
        try
        {
            // Receive message from client
            byte[] buffer = new byte[8192];
            int received = client.Receive(buffer);
            // Convert bytes to string
            string json_receive = Encoding.UTF8.GetString(buffer, 0, received);
            Client_Response client_response = null;
            try
            {
                // Convert string to an object
                client_response = JsonSerializer.Deserialize<Client_Response>(json_receive);
            }
            catch (Exception e)
            {
                if (json_receive == "SRF")
                {
                    srf = new SRF(client);
                    lock (print_lock)
                    {
                        Console.WriteLine("Super Render Farm connected!");
                    }
                    return;
                }

                Write_Log(e.ToString());
            }

            while (use_srf && srf == null)
            {
                lock (print_lock)
                {
                    Console.WriteLine("SRF not connected, waiting");
                    Write_Log("SRF not connected, waiting");
                }
                Thread.Sleep(1000);
            }

            // Prepare a new response and string
            Master_Response master_response = new Master_Response();
            string json_send;

            // Find out what the client wants
            if (client_response.message == "new")
            {
                // If Client wants work, check if frames are left
                List<int> frames = Aquire_Frames(client_response);

                master_response.message = "NAN";

                // If frames left, then provide Client with information about the project
                if (frames.Count != 0)
                {
                    master_response.message = "here";
                    master_response.id = PROJECT.id;
                    master_response.file_size = new FileInfo(PROJECT.full_path_blend).Length;
                    master_response.first_frame = frames.First();
                    master_response.last_frame = frames.Last();
                    master_response.frame_step = PROJECT.frame_step;
                    master_response.render_engine = PROJECT.render_engine;
                    master_response.file_format = PROJECT.output_file_format;
                    master_response.use_sid_temporal = PROJECT.use_sid_temporal;
                }

                // Convert the object to string
                json_send = JsonSerializer.Serialize(master_response);
                // Convert string to bytes
                byte[] bytes_send = Encoding.UTF8.GetBytes(json_send);
                // Send bytes to client
                client.Send(bytes_send);

                if (master_response.message == "NAN")
                {
                    // Cut the connection
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();

                    string log = "no frames assigned to: " + client_ip;
                    lock (print_lock)
                    {
                        Console.WriteLine(log);
                        Write_Log(log);
                    }

                    return;
                }

                else
                {
                    string log = string.Format("Frame {0} - {1} assigned to {2}",
                                        master_response.first_frame,
                                        master_response.last_frame,
                                        client.RemoteEndPoint.ToString());
                    lock (print_lock)
                    {
                        Console.WriteLine(log);
                        Write_Log(log);
                    }
                }

                // Receive message from client
                buffer = new byte[1024];
                received = client.Receive(buffer);
                // Convert bytes to string
                json_receive = Encoding.UTF8.GetString(buffer, 0, received);
                // Convert string to an object
                client_response = JsonSerializer.Deserialize<Client_Response>(json_receive);

                // If the client doesn't have the .Blend, send it to him
                if (client_response.message == "needed")
                {
                    client.SendFile(PROJECT.full_path_blend);
                }

                if (srf != null)
                {
                    foreach (int frame in frames)
                    {
                        srf.Send_Update(frame, "rendering");
                    }
                }
            }

            else if (client_response.message == "output")
            {
                // If Client is done rendering execute this
                // Check if Clients work is any good
                // If it isn't add his frames back
                if (!client_response.faulty.Contains(false))
                {
                    Add_Frames(client_response.frames);

                    if (srf != null)
                    {
                        foreach (int frame in client_response.frames)
                        {
                            srf.Send_Update(frame, "waiting");
                        }
                    }
                }

                else
                {
                    // Send a message to the Client to syncronize
                    byte[] bytes_send = Encoding.UTF8.GetBytes("drop");
                    client.Send(bytes_send);

                    // Generate a file name
                    string zip_file = client_response.frames.First() + "_" + client_response.frames.Last() + ".zip";
                    // Generate a path for the file
                    string path = Path.Join(PROJECT_DIRECTORY, zip_file);
                    // Download the file into the path
                    using (FileStream file_stream = File.Create(path))
                    {
                        new NetworkStream(client).CopyTo(file_stream);
                    }
                    // Extract the contents
                    ZipFile.ExtractToDirectory(path, PROJECT_DIRECTORY);

                    string log = string.Format("Frame {0} - {1} downloaded from {2}",
                                               client_response.frames.First(),
                                               client_response.frames.Last(),
                                               client.RemoteEndPoint.ToString());
                    lock (print_lock)
                    {
                        Console.WriteLine(log);
                        Write_Log(log);
                    }

                    // Initialize list for checking "good" frames
                    List<int> done = new List<int>();
                    // Initialize list for checking "bad" frames
                    List<int> bad = new List<int>{};

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

                    // Check for every frame
                    foreach (string file in client_response.files)
                    {
                        // If the file exsists add it to list
                        if (File.Exists(Path.Join(base_directory, file)))
                        {
                            // Index for every file and the frame is the same,
                            // so use position of file for the frame
                            done.Add(client_response.frames[client_response.files.IndexOf(file)]);

                            if (srf != null)
                            {
                                srf.Send_Update(client_response.frames[client_response.files.IndexOf(file)], "done");
                            }
                        }

                        else
                        {
                            bad.Add(client_response.frames[client_response.files.IndexOf(file)]);

                            if (srf != null)
                            {
                                srf.Send_Update(client_response.frames[client_response.files.IndexOf(file)], "waiting");
                            }
                        }
                    }

                    log = string.Format("Frame {0} - {1} rendered!",
                                        client_response.frames.First(),
                                        client_response.frames.Last());
                    lock (print_lock)
                    {
                        Console.WriteLine(log);
                        Write_Log(log);
                    }

                    // Append the completed frames
                    Add_Frames_Done(done);
                    // Append the bad frames
                    Add_Frames(bad);

                    //Update_View(progress_bar.Update_Progress_Bar(done.Count));
                    lock (print_lock)
                    {
                        Console.WriteLine(progress_bar.Update_Progress_Bar(done.Count));
                    }
                }
            }

            else if (client_response.message == "ping")
            {
                // If Client is pinging master
                // Return "pong"
                master_response.message = "pong";
                // Convert the object to string
                json_send = JsonSerializer.Serialize(master_response);
                // Convert string to bytes
                byte[] bytes_send = Encoding.UTF8.GetBytes(json_send);

                lock (print_lock)
                {
                    Console.WriteLine("Received ping! Returning pong");
                    Write_Log("Received ping! Returning pong");
                }

                client.Send(bytes_send);
            }

            // Cut the connection
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        catch (Exception e)
        {
            // Log errors
            lock (print_lock)
            {
                Console.WriteLine("An error occurred, trying to continue anyways... (see the log files for more details)");
                Write_Log(e.ToString());
            }
        }
    }
    // Re-add frames to the frames remaining
    public static void Add_Frames(List<int> frames)
    {
        // Make sure to only change a thing at once
        lock (frame_lock)
        {
            // Append all frames from list
            foreach (int frame in frames)
            {
                frames_left.Add(frame);
            }
        }
    }
    // Get the frames currently left
    public static List<int> Get_Frames()
    {
        // Make sure to only change a thing at once
        // Return the frames left
        lock (frame_lock)
        {
            return frames_left;
        }
    }
    // Aquire frames for the Client
    public static List<int> Aquire_Frames(Client_Response requirements)
    {
        // Have an empty list ready
        List<int> empty_list = new List<int>{};

        // If Client Blender version does not match
        // Don't give him frames
        if (requirements.blender_version != PROJECT.blender_version)
        {
            return empty_list;
        }

        // If Client is not allowed to use engine
        // Don't give him frames
        if (!requirements.allowed_engines.Contains(PROJECT.render_engine) && !requirements.allowed_engines.Contains("other"))
        {
            return empty_list;
        }

        // If Client doesn't allow for as much time
        // Don't give him frames
        if (requirements.limit_time_frame != 0)
        {
            if (requirements.limit_time_frame < PROJECT.time_per_frame)
            {
                return empty_list;
            }
        }

        // If Client doesn't want to use as much RAM
        // Don't give him frames
        if (requirements.limit_ram_use != 0)
        {
            if (requirements.limit_ram_use < PROJECT.ram_use)
            {
                return empty_list;
            }
        }

        // Make sure to only change a thing at once
        lock (frame_lock)
        {
            if (frames_left.Count == 0)
            {
                return empty_list;
            }

            // Create a list with frames for Client
            List<int> frames_picked = new List<int> { };
            // Set the first frame to the first frame left
            int first_frame = frames_left.Min();

            // Itterate as long as we are within the batch size
            for (int batch = 0; batch <= PROJECT.batch_size* PROJECT.frame_step; batch += PROJECT.frame_step)
            {
                // If there are no frames left, return the picked ones
                if (frames_left.Count == 0)
                {
                    return frames_picked;
                }

                // If out frame is left, add it to the picked frames
                // and remove it from the frames left
                if (frames_left.Contains(first_frame + batch))
                {
                    frames_picked.Add(first_frame + batch);
                    frames_left.Remove(first_frame + batch);
                }

                // If there is not another continous frames
                // return the picked ones
                else
                {
                    return frames_picked;
                }
            }

            // Fail safe
            return frames_picked;
        }
    }

    // Add a completed frame
    public static void Add_Frames_Done(List<int> frames)
    {
        // Make sure to only change a thing at once
        lock (done_lock)
        {
            foreach (int frame in frames)
            {
                PROJECT.frames_complete.Add(frame);
            }

            Save_Project();
        }
    }
    // Get the frames done
    public static List<int> Get_Frames_Done()
    {
        // Make sure to only change a thing at once
        lock (done_lock)
        {
            return PROJECT.frames_complete;
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
    public static void Show_Top_Bar(List<string> addition = null)
    {
        lock (print_lock)
        {
            Console.WriteLine("Pidgeon Render Farm - Master");
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
    }

    public static void Update_View(string progress_bar)
    {
        Console.Clear();
        Show_Top_Bar(new List<string> {"Master IP address: " + ip_address_string
                                       , "Master Port: " + SETTINGS.port.ToString()
                                       , progress_bar});

        foreach (string line in File.ReadLines(BACKUP_FILE))
        {
            Console.WriteLine(line);
        }
    }

    public static void Print(string content)
    {
        lock (print_lock)
        {
            Console.WriteLine(content);
            File.AppendAllText(BACKUP_FILE, content);
        }
    }
    #endregion

    #region Setup
    public static void First_Time_Setup()
    {
        // Create empty settings object
        Settings new_settings = new Settings();
        // Removes the need to type "new List<string> { "Yes", "No" }" every time
        List<string> basic_bool = new List<string> { "Yes", "No" };

        // Write version - for updating in the future
        new_settings.version = VERSION;

        // Enable logging
        // Use Menu() to grab user input
        new_settings.enable_logging = Parse_Bool(Menu(basic_bool, new List<string> { "Enable logging? (It is recommended to turn this on. To see whats included please refer to the documentation!)" }));

        // Port
        // Let the user input a valid port
        // If emtpy, then use default port
        Show_Top_Bar();
        Console.WriteLine("Which Port to use? (Default: 19186):");
        string user_input = Console.ReadLine();
        while (!Is_Port(user_input))
        {
            if (user_input == "")
            {
                user_input = "19186";
                break;
            }

            Console.WriteLine("Please input a whole number between 1 and 65536");
            user_input = Console.ReadLine();
        }
        new_settings.port = Math.Abs(int.Parse(user_input));
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
        Console.Clear();

        // FFMPEG Executable
        // Check if the file exsists
        /*Show_Top_Bar();
        Console.WriteLine("Where is your FFMPEG executable stored? (if you don't want to use FFMPEG just leave this empty)");
        user_input = Console.ReadLine().Replace("\"", "");
        while (!File.Exists(user_input) && user_input != "")
        {
            Console.WriteLine("Please input the path to your FFMPEG executable");
            user_input = Console.ReadLine().Replace("\"", "");
        }
        new_settings.ffmpeg_executable = user_input;*/

        // Keep output
        // Use Menu() to grab user input
        //new_settings.keep_output = Parse_Bool(Menu(basic_bool, new List<string> { "Keep the files received from the clients?" }));

        // Use FTP
        // Use Menu() to grab user input
        //new_settings.use_ftp = Parse_Bool(Menu(basic_bool, new List<string> { "Use 'File Transfer Protocol' instead of sockets for file distribution?" }));

        // Data collection
        // Use Menu() to grab user input
        new_settings.collect_data = Parse_Bool(Menu(basic_bool, new List<string> { "Allow us to collect data? (it is only stored locally for debugging purposes)" }));

        // Save the settings
        Save_Settings(new_settings);
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

    #region Project_Setup
    // Run the setup for the project
    public static void Project_Setup()
    {
        // Create empty project
        Project new_project = new Project();

        List<string> basic_bool = new List<string> { "Yes", "No" };

        // +1 on data file
        Save_Data();
        // Generate ID based on project number
        new_project.id = PRF_DATA.projects.ToString();

        // Blend file
        // Let user input a valid Blender project file
        Show_Top_Bar();
        Console.WriteLine("Where is your .blend stored? (Be sure to actually use a .blend)");
        string user_input = Console.ReadLine().Replace("\"", "");
        while (!File.Exists(user_input) && !user_input.EndsWith(".blend"))
        {
            Console.WriteLine("Please input the path to your .blend");
            user_input = Console.ReadLine().Replace("\"", "");
        }
        new_project.full_path_blend = user_input;

        // Use SuperFastRender
        // Use Menu() to grab user input
        bool use_sfr = Parse_Bool(Menu(basic_bool, new List<string> { "Use SuperFastRender (with default settings) to optimize the rendering process?" }));

        // Use SuperFastRender
        // Use Menu() to grab user input
        //new_project.use_sid_temporal = Parse_Bool(Menu(basic_bool, new List<string> { "Use SuperImageDenoiser Temporal (with default settings) for denoising?" }));

        // Render test frame (for time)
        // Use Menu() to grab user input
        bool test_render = Parse_Bool(Menu(basic_bool,
                                           new List<string> { "Render a test frame? (Will take some time, for client option 'Maximum time per frame')" }));

        // Batch size
        // Let user input a valid size
        Show_Top_Bar();
        Console.WriteLine("Batch size:");
        user_input = Console.ReadLine();
        while (!int.TryParse(user_input, out _))
        {
            Console.WriteLine("Please input a whole number");
            user_input = Console.ReadLine();
        }
        new_project.batch_size = Math.Abs(int.Parse(user_input));
        Console.Clear();

        // Use ZIP
        // Use Menu() to grab user input
        //new_project.use_zip = Parse_Bool(Menu(basic_bool, new List<string> { "Zip/compress files before distributing? (might save some bandwitdh at the cost of image quality; recommended to use with Batches)" }));

        // Generate a video file
        // Use Menu() to grab user input
        //new_project.video_generate = Parse_Bool(Menu(basic_bool,
        //                                             new List<string> { "Generate a video file? (MP4-Format, FFMPEG has to be installed!)" }));

        new_project.video_generate = false;

        // Only required if we generate a video
        if (new_project.video_generate)
        {
            // Video FPS
            // Let user input a valid value
            Show_Top_Bar();
            Console.WriteLine("Video frames per second: (Default: 24)");
            user_input = Console.ReadLine();
            while (!int.TryParse(user_input, out _))
            {
                if (user_input == "")
                {
                    user_input = "24";
                    break;
                }

                Console.WriteLine("Please input a whole number");
                user_input = Console.ReadLine();
            }
            new_project.video_fps = Math.Abs(int.Parse(user_input));
            Console.Clear();

            // Video rate control type
            // Use Menu() to grab user input
            new_project.video_rate_control = Menu(new List<string> { "CRF", "CBR" },
                                                  new List<string> { "What Video Rate Control to use?" });

            // Video rate control value (e.g. bitrate)
            // Let user input a valid value
            Show_Top_Bar();
            Console.WriteLine("Video Rate Control Value: (CRF - lower is better; CBR - higher is better)");
            user_input = Console.ReadLine();
            while (!int.TryParse(user_input, out _))
            {
                Console.WriteLine("Please input a whole number");
                user_input = Console.ReadLine();
            }
            new_project.video_rate_control_value = Math.Abs(int.Parse(user_input));
            Console.Clear();

            // Resize the video
            // Use Menu() to grab user input
            new_project.video_resize = Parse_Bool(Menu(new List<string> { "Yes", "No" },
                                                       new List<string> { "Rescale the video?" }));

            if (new_project.video_resize)
            {
                // New video witdh/x
                // Let user input a valid resolution
                Show_Top_Bar();
                Console.WriteLine("New video width:");
                user_input = Console.ReadLine();
                while (!int.TryParse(user_input, out _))
                {
                    Console.WriteLine("Please input a whole number");
                    user_input = Console.ReadLine();
                }
                new_project.video_x = Math.Abs(int.Parse(user_input));
                Console.Clear();

                // New video height/y
                // Let user input a valid resolution
                Show_Top_Bar();
                Console.WriteLine("New video height:");
                user_input = Console.ReadLine();
                while (!int.TryParse(user_input, out _))
                {
                    Console.WriteLine("Please input a whole number");
                    user_input = Console.ReadLine();
                }
                new_project.video_y = Math.Abs(int.Parse(user_input));
                Console.Clear();
            }
        }

        // Get project directory name and create it
        PROJECT_DIRECTORY = Path.Join(SCRIPT_DIRECTORY, new_project.id);
        Directory.CreateDirectory(PROJECT_DIRECTORY);

        Show_Top_Bar();
        Console.WriteLine("Gathering informations of your project. This usually only takes a few seconds. Please wait...");

        // Create a command for blender to optain some variables
        string args = string.Format("-b \"{0}\" -P BPY.py -- {1} {2}",
                             new_project.full_path_blend,
                             Bool_To_Int(use_sfr),
                             Bool_To_Int(test_render));

        // Use Blender to obtain informations about the project
        // additionally use SFR if selected
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

        // Read the output
        string json_string = File.ReadAllText(Path.Join(Path.GetDirectoryName(new_project.full_path_blend), "vars.json"));
        Project_Data project_data = JsonSerializer.Deserialize<Project_Data>(json_string);

        File.Delete(Path.Join(Path.GetDirectoryName(new_project.full_path_blend), "vars.json"));

        // Apply the values to project object
        new_project.blender_version = project_data.blender_version;
        new_project.render_engine = project_data.render_engine;
        new_project.time_per_frame = project_data.render_time;
        new_project.output_file_format = project_data.file_format;
        new_project.first_frame = project_data.first_frame;
        new_project.last_frame = project_data.last_frame;
        new_project.frame_step = project_data.frame_step;
        new_project.frames_total = 0;

        // Append every frame to frames_left
        for (int frame = new_project.first_frame; frame <= new_project.last_frame; frame += new_project.frame_step)
        {
            frames_left.Add(frame);
            new_project.frames_total++;
        }

        // If the file exsists add it to list
        string file = "frame_" + new_project.first_frame.ToString().PadLeft(6, '0') + "." + new_project.output_file_format;
        if (File.Exists(Path.Join(Path.GetDirectoryName(new_project.full_path_blend), file)))
        {
            File.Move(Path.Join(Path.GetDirectoryName(new_project.full_path_blend), file), Path.Join(PROJECT_DIRECTORY, file));
            frames_left.Remove(new_project.first_frame);
            new_project.frames_complete.Add(new_project.first_frame);
        }

        // Save the project
        Save_Project(new_project);

        // Start rendering
        Render_Project();
    }

    // Save the project
    public static void Save_Project(Project new_project)
    {
        // Update the global object
        PROJECT = new_project;

        // Convert object to json
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(PROJECT, options);

        // Write to file
        File.WriteAllText(Path.Combine(PROJECT_DIRECTORY, PROJECT.id + PROJECT_EXTENSION), jsonString);
    }
    // Save the current PROJECT
    public static void Save_Project()
    {
        // Convert object to json
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(PROJECT, options);

        // Write to file
        File.WriteAllText(Path.Combine(PROJECT_DIRECTORY, PROJECT.id + PROJECT_EXTENSION), jsonString);
    }

    // Load a project
    public static void Load_Project(string project_file)
    {
        // Prevent file read errors
        if (!File.Exists(project_file))
        {
            Project_Setup();
        }

        // Read json from file
        // Convert json to object
        // Update global project object
        // Update global project directory
        string json_string = File.ReadAllText(project_file);
        PROJECT = JsonSerializer.Deserialize<Project>(json_string);
        PROJECT_DIRECTORY = Path.Join(SCRIPT_DIRECTORY, PROJECT.id);

        // Add all frames left to render
        frames_left = new List<int>();
        for (int frame = PROJECT.first_frame; frame <= PROJECT.last_frame; frame += PROJECT.frame_step)
        {
            if (!PROJECT.frames_complete.Contains(frame))
            {
                frames_left.Add(frame);
            }
        }

        Render_Project();
    }
    // Load a project from a given json string
    public static void Load_Project_From_String(string json_string)
    {
        try
        {
            // Convert json to object
            // Update global project object
            // Update global project directory
            PROJECT = JsonSerializer.Deserialize<Project>(json_string);
            // +1 on data file
            Save_Data();
            // Generate ID based on project number
            PROJECT.id = PRF_DATA.projects.ToString();
            // Use ID to create a directory for the preoject
            PROJECT_DIRECTORY = Path.Join(SCRIPT_DIRECTORY, PROJECT.id);
            Directory.CreateDirectory(PROJECT_DIRECTORY);

            // When passing from SFR we won't have frames rendered
            PROJECT.frames_complete = new List<int>();

            // Add all frames left to render
            frames_left = new List<int>();
            for (int frame = PROJECT.first_frame; frame <= PROJECT.last_frame; frame += PROJECT.frame_step)
            {
                frames_left.Add(frame);
            }

            // If the file exsists add it to list
            string file = "frame_" + PROJECT.first_frame.ToString().PadLeft(6, '0') + "." + PROJECT.output_file_format;
            if (File.Exists(Path.Join(SCRIPT_DIRECTORY, file)))
            {
                File.Move(Path.Join(SCRIPT_DIRECTORY, file), Path.Join(PROJECT_DIRECTORY, file));

                frames_left.Remove(PROJECT.first_frame);
                Add_Frames_Done(new List<int>{ PROJECT.first_frame });
            }

            // Save the project
            Save_Project();

            use_srf = false;

            Render_Project();
        }
        catch (Exception e)
        {
            // Log errors
            Console.WriteLine(e.ToString());
            Write_Log(e.ToString());
        }
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

    // Collect system data
    public static void Collect_Data()
    {
        if (!File.Exists(DATA_FILE))
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

        else
        {
            Load_Data();
        }
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

    // Add 1 to projects variable
    public static void Save_Data()
    {
        // +1 here
        PRF_DATA.projects = PRF_DATA.projects + 1;

        // Convert object to json
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json_string = JsonSerializer.Serialize(PRF_DATA, options);

        // Write json to file
        File.WriteAllText(DATA_FILE, json_string);
    }

    // Load PRF_DATA
    public static void Load_Data()
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
        // If it is a human yes, return computer true
        if (new List<string>() { "true", "yes", "y", "1" }.Any(s => s.Contains(value.ToLower())))
        {
            return true;
        }

        // If it is a human no, return computer false
        else if (new List<string>() { "false", "no", "n", "0" }.Any(s => s.Contains(value.ToLower())))
        {
            return false;
        }

        // If human is not sure, return default value
        else if (new List<string>() { "", null }.Any(s => s.Contains(value.ToLower())))
        {
            return def;
        }

        // if none applies, return error
        return null;
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

    public static int Bool_To_Int(bool input)
    {
        if (input)
        {
            return 1;
        }

        return 0;
    }
    #endregion
}

public class SRF
{
    public Socket srf_client;

    public SRF(Socket client)
    {
        srf_client = client;
    }

    public void Send_Update(int frame, string state)
    {
        // Convert the object to string
        string json_send = frame.ToString() + "|" + state;
        // Convert string to bytes
        byte[] bytes_send = Encoding.UTF8.GetBytes(json_send);
        // Send bytes to client
        srf_client.Send(bytes_send);

        // Receive message from client
        byte[] buffer = new byte[64];
        srf_client.Receive(buffer);
    }

    public void Finish()
    {
        srf_client.Shutdown(SocketShutdown.Both);
        srf_client.Close();
    }
}

public class Progress_Bar
{
    public int progress;
    public int goal;

    public string i;
    public char b;
    public char p_c;
    public char l_c;

    public Progress_Bar(int part, int max, string info = "", char border = '|', char progress_char = '#', char left_char = ' ')
    {
        progress = part;
        goal = max;
        i = info;
        b = border;
        p_c = progress_char;
        l_c = left_char;
    }

    public string Get_Progress_Bar()
    {
        float percent = (progress / 1.0f) / (goal / 1.0f) * 100;
        int percent_rounded = (int)Math.Round(percent);
        int bar_max = Console.WindowWidth - 6 - i.Length;
        int bar_factor = (int)Math.Floor(bar_max / 1.0f / goal);

        //100%|########|
        // 50%|####    |
        //  0%|        |

        string bar = i;
        bar += percent_rounded.ToString().PadLeft(3, ' ') + "%";
        bar += b;
        bar += string.Concat(Enumerable.Repeat(p_c, progress * bar_factor));
        bar += "".PadRight((goal - progress) * bar_factor, l_c);
        bar += b;

        return bar;
    }

    public string Update_Progress_Bar(int step)
    {
        progress += step;
        float percent = (progress / 1.0f) / (goal / 1.0f) * 100;
        int percent_rounded = (int)Math.Round(percent);
        int bar_max = Console.WindowWidth - 6 - i.Length;
        int bar_factor = (int)Math.Floor(bar_max / 1.0f / goal);

        //100%|########|
        // 50%|####    |
        //  0%|        |

        string bar = i;
        bar += percent_rounded.ToString().PadLeft(3, ' ') + "%";
        bar += b;
        bar += string.Concat(Enumerable.Repeat(p_c, progress * bar_factor));
        bar += "".PadRight((goal - progress) * bar_factor, l_c);
        bar += b;

        return bar;
    }
}

#region Objects
// Settings object class
public class Settings
{
    public string version { get; set; } = "0.0.0";
    public int port { get; set; } = 8080;
    public string blender_executable { get; set; }
    public string ffmpeg_executable { get; set; }
    public bool keep_output { get; set; }
    public bool use_ftp { get; set; }
    public bool collect_data { get; set; }
    public bool enable_logging { get; set; }
}

// Project object class
public class Project
{
    public string id { get; set; }
    public string blender_version { get; set; }
    public string full_path_blend { get; set; }
    public bool use_sid_temporal { get; set; } = false;
    public string render_engine { get; set; }
    public string output_file_format { get; set; }
    public bool video_generate { get; set; }
    public int video_fps { get; set; }
    public string video_rate_control { get; set; } // CBR, CRF
    public int video_rate_control_value { get; set; }
    public bool video_resize { get; set; }
    public int video_x { get; set; }
    public int video_y { get; set; }
    public int batch_size { get; set; } = 0;
    public float time_per_frame { get; set; }
    public int ram_use { get; set; } = 0;
    public int first_frame { get; set; }
    public int last_frame { get; set; }
    public int frame_step { get; set; } = 1;
    public int frames_total { get; set; }
    public List<int> frames_complete { get; set; } = new List<int>();
}

// Communication from Client
public class Client_Response
{
    public string message { get; set; }
    public string blender_version { get; set; }
    public List<string> allowed_engines { get; set; }
    public int limit_ram_use { get; set; }
    public float limit_file_size { get; set; }
    public float limit_time_frame { get; set; }
    public List<bool> faulty { get; set; }
    public List<int> frames { get; set; }
    public List<string> files { get; set; }
}

// Communication to Client
public class Master_Response
{
    public string message { get; set; }
    public bool use_ftp { get; set; } = false;
    public bool use_sid_temporal { get; set; } = false;
    public string id { get; set; }
    public float file_size { get; set; }
    public string render_engine { get; set; }
    public string file_format { get; set; }
    public int first_frame { get; set; }
    public int last_frame { get; set; }
    public int frame_step { get; set; } = 1;

}

// Project_Data object class
public class Project_Data
{
    public string blender_version { get; set; }
    public string render_engine { get; set; }
    public float render_time { get; set; } = 0.0f;
    public int ram_use { get; set; } = 0;
    public string file_format { get; set; }
    public int first_frame { get; set; }
    public int last_frame { get; set; }
    public int frame_step { get; set; }
}

// PRF_Data object class
public class PRF_Data
{
    public string os { get; set; } = "No data";
    public List<string> cpus { get; set; } = new List<string> { "No data" };
    public List<string> gpus { get; set; } = new List<string> { "No data" };
    public int ram { get; set; } = 0;
    public int projects { get; set; } = 0;
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
