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
    public static string Bin_Directory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
    public static string Database_Directory = "";
    public static string Project_Directory = "";

    // Create global objects and variables
    public static string IP_Address_String = "127.0.0.1";
    public static ClientSettings Settings;
    private static object Log_DB_Lock = new object();
    public static ProgressBar Progress_Bar;

    public static WebClient Web_Client;

    private static SettingsFileHandler Settings_File_Handler;
    #endregion

    // Runs at start
    static void Main(string[] args)
    {
        Client client = new Client();
        client.Start(args);
    }
    public void Start(string[] args)
    {
        // Get log directory name and create it
        Database_Directory = Path.Join(Bin_Directory, "Database");
        if (!Directory.Exists(Database_Directory))
        {
            Directory.CreateDirectory(Database_Directory);
        }

        Settings_File_Handler = new SettingsFileHandler(Path.Join(Bin_Directory, "client_settings.json"));
        try
        {
            Settings = Settings_File_Handler.Load_Client_Settings();
        }
        catch (FileNotFoundException)
        {
            // Run setup
            First_Time_Setup();
        }
        catch (FileLoadException)
        {
            // Run setup
            First_Time_Setup();
        }

        DBHandler.Initialize(Settings.Database_Connection);

        new SystemInfo(Settings.Allow_Data_Collection, Bin_Directory);

        Main_Menu();
    }

    public void Main_Menu()
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

        Menu menu = new Menu(items, "Client");

        while (true)
        {
            // Show the Menu and grab the selection
            string selection = menu.Show();

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
    public void Worker()
    {
        Display.Show_Top_Bar("Client", new List<string> { $"Client IP address: {Helpers.Get_IPv4()}" });

        int rendered_frames = 0;
        DateTime start_time = DateTime.Now;
        int failed_connections = 0;

        while (true)
        {
            #region Shutdown Parameters
            if (Settings.Shutdown_Parameters.Rendered_Frames != 0)
            {
                if (rendered_frames >= Settings.Shutdown_Parameters.Rendered_Frames)
                {
                    break;
                }
            }
            if (Settings.Shutdown_Parameters.Time != new TimeSpan(0))
            {
                if (DateTime.Now - start_time >= Settings.Shutdown_Parameters.Time)
                {
                    break;
                }
            }
            if (Settings.Shutdown_Parameters.Failed_Connections != 0)
            {
                if (failed_connections >= Settings.Shutdown_Parameters.Failed_Connections)
                {
                    break;
                }
            }
            #endregion

            foreach (MasterConnection master in Settings.Master_Connections)
            {
                while (true)
                {
                    try
                    {
                        // Parse master ip
                        IPAddress ip_address = IPAddress.Parse(master.IPv4);
                        // Add master as end point using ip and port
                        IPEndPoint remote_end_point = new IPEndPoint(ip_address, master.Port);
                        // Socket for connection -> TCP
                        Socket connection = new Socket(ip_address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                        try
                        {
                            string log = "";

                            DateTime connecting_time = DateTime.Now;
                            Logger.Log(this, $"Trying to connect to: {master.IPv4}:{master.Port}");

                            // Connect to Master
                            connection.Connect(remote_end_point);

                            // Log the time and ip of the Master connection
                            DateTime connection_time = DateTime.Now;
                            Console.WriteLine($"Connected to: {connection.RemoteEndPoint} @ {connection_time}");
                            Logger.Log(this, $"Connected to: {connection.RemoteEndPoint}");

                            // Prepare request
                            ClientResponse client_response = new ClientResponse();

                            // Request a job
                            client_response.Message = "new";
                            client_response.Blender_Installations = Settings.Blender_Installations;
                            client_response.RAM_Use_Limit = Settings.RAM_Use_Limit;
                            client_response.Render_Time_Limit = Settings.Render_Time_Limit;
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
                            MasterResponse master_response = JsonSerializer.Deserialize<MasterResponse>(json_receive);

                            if (master_response.Message == "NAN")
                            {
                                Console.WriteLine($"{connection.RemoteEndPoint}: no new job");
                                Logger.Log(this, $"{connection.RemoteEndPoint}: no new job");
                                connection.Shutdown(SocketShutdown.Both);
                                connection.Close();
                                Thread.Sleep(20000);
                                continue;
                            }
                            else
                            {
                                Console.WriteLine($"{connection.RemoteEndPoint}; Project: {master_response.ID}; Frame {master_response.Frames.First().Id} - {master_response.Frames.Last().Id}");
                                Logger.Log(this, $"{connection.RemoteEndPoint}; Project: {master_response.ID}; Frame {master_response.Frames.First().Id} - {master_response.Frames.Last().Id}");
                            }

                            Project_Directory = Path.Join(Bin_Directory, master_response.ID.ToString());

                            // Check if the directory exsists
                            if (!Directory.Exists(Project_Directory))
                            {
                                // If not -> create it
                                Directory.CreateDirectory(Project_Directory);
                            }

                            // Generate .blend file name
                            string blend_file = Path.Join(Project_Directory, (master_response.ID + ".blend"));
                            // grab it's size
                            long file_size = 0;
                            try
                            {
                                file_size = new FileInfo(blend_file).Length;
                            }
                            catch { }

                            // Compare if out file is the same as the Master's
                            if (!File.Exists(blend_file) || file_size != master_response.File_Size)
                            {
                                if (master_response.File_transfer_Mode == FileTransferMode.TCP)
                                {
                                    // Prepare request
                                    client_response = new ClientResponse();

                                    // If it's not the same
                                    // Tell the Master we still need the file
                                    client_response.Message = "needed";
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

                                    Logger.Log(this, $"Downloaded {blend_file}");
                                }
                                
                                else if (master_response.File_transfer_Mode == FileTransferMode.SMB)
                                {
                                    File.Copy(Path.Join(master_response.Connection_String, (master_response.ID + ".blend")), blend_file);

                                    Logger.Log(this, $"Downloaded {blend_file}");
                                }

                                else if (master_response.File_transfer_Mode == FileTransferMode.FTP)
                                {
                                    Web_Client = new WebClient();
                                    //Web_Client.Credentials = new NetworkCredential(Settings.FTP_Connection.User, Settings.FTP_Connection.Password);
                                    Web_Client.DownloadFile(Path.Join(master_response.Connection_String, (master_response.ID + ".blend")), blend_file);

                                    Logger.Log(this, $"Downloaded {blend_file}");
                                }
                            }

                            else
                            {
                                if (master_response.File_transfer_Mode == FileTransferMode.TCP)
                                {
                                    // If it's the same just send a message to drop
                                    client_response.Message = "drop";
                                    // Convert the object to string
                                    json_send = JsonSerializer.Serialize(client_response);
                                    // Convert string to bytes
                                    bytes_send = Encoding.UTF8.GetBytes(json_send);
                                    // Send bytes to Master
                                    connection.Send(bytes_send);

                                    Console.WriteLine($"{blend_file} already exists");
                                    Logger.Log(this, $"{blend_file} already exists");
                                }
                            }

                            // Cut the connection
                            connection.Shutdown(SocketShutdown.Both);
                            connection.Close();

                            if (master_response.Use_SID_Temporal)
                            {
                                Render_SID_Temporal(blend_file,
                                                   master_response.Frames.First().Id,
                                                   master_response.Frames.Last().Id,
                                                   master_response.Render_Engine);
                            }
                            else
                            {
                                Render(blend_file,
                                       master_response.Frames.First().Id,
                                       master_response.Frames.Last().Id,
                                       master_response.Render_Engine,
                                       master_response.File_Format);
                            }

                            Console.WriteLine($"Frame {master_response.Frames.First().Id} - {master_response.Frames.Last().Id} rendered!");
                            Logger.Log(this, $"Frame {master_response.Frames.First().Id} - {master_response.Frames.Last().Id} rendered!");

                            // Prepare new response
                            client_response = new ClientResponse();
                            client_response.Message = "output";
                            client_response.Frames = new List<Frame>();

                            // List for paths -> send files
                            List<string> paths = new List<string>();

                            string base_directory = Project_Directory;

                            if (master_response.Use_SID_Temporal)
                            {
                                if (Directory.Exists(Path.Join(Project_Directory, "noisy")))
                                {
                                    base_directory = Path.Join(Project_Directory, "noisy");
                                }
                                else if (Directory.Exists(Path.Join(Project_Directory, "processing")))
                                {
                                    base_directory = Path.Join(Project_Directory, "processing");
                                }

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
                            for (int frame_id = master_response.Frames.First().Id; frame_id <= master_response.Frames.Last().Id; frame_id += master_response.Frame_Step)
                            {
                                Frame frame = new Frame();

                                string file_name = "";
                                string path = "";

                                if (master_response.Use_SID_Temporal)
                                {
                                    // Generate the file name
                                    file_name = frame_id.ToString().PadLeft(6, '0') + ".exr";
                                    // Add file to list
                                    frame.File_Name = file_name;
                                    // Get it's path
                                    path = Path.Join(base_directory, file_name);
                                }
                                else
                                {
                                    // Generate the file name
                                    file_name = "frame_" + frame_id.ToString().PadLeft(6, '0') + "." + master_response.File_Format;
                                    // Get it's path
                                    path = Path.Join(Project_Directory, file_name);
                                    // Add file to list
                                    frame.File_Name = file_name;
                                    // Add path to list
                                    paths.Add(path);
                                }

                                // Set faulty value based on exsistance
                                frame.Quality = File.Exists(path);

                                frame.Id = frame_id;

                                // Add frame to list
                                client_response.Frames.Add(frame);
                            }

                            // Prepare zip file name and path
                            string zip_name = master_response.Frames.First().Id + "_" + master_response.Frames.Last().Id + ".zip";
                            string zip_file = Path.Join(Project_Directory, zip_name);

                            // Add frames to ZIP
                            using (ZipArchive archive = ZipFile.Open(zip_file, ZipArchiveMode.Create))
                            {
                                if (!master_response.Use_SID_Temporal)
                                {
                                    foreach (string path in paths)
                                    {
                                        archive.CreateEntryFromFile(path, Path.GetFileName(path));
                                    }
                                }
                                else
                                {
                                    base_directory = Project_Directory;
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
                                if (client_response.Frames.Where(frame => frame.Quality == true).ToList().Count > 0)
                                {
                                    if (master_response.File_transfer_Mode == FileTransferMode.TCP)
                                    {
                                        // Synchronize with Master
                                        buffer = new byte[1024];
                                        connection.Receive(buffer);

                                        // Send the ZIP file
                                        connection.SendFile(zip_file);
                                    }
                                    else if (master_response.File_transfer_Mode == FileTransferMode.SMB)
                                    {
                                        File.Copy(zip_file, Path.Join(master_response.Connection_String, zip_name));
                                    }
                                    else if (master_response.File_transfer_Mode == FileTransferMode.FTP)
                                    {
                                        Web_Client.UploadFile(Path.Join(master_response.Connection_String, zip_name), zip_file);
                                    }

                                    log = string.Format("Frame {0} - {1} uploaded!", master_response.Frames.First().Id, master_response.Frames.Last().Id);
                                    Console.WriteLine(log);
                                    Logger.Log(this, log);
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log errors
                                Logger.Log(this, ex.ToString());
                            }

                            // Cut the connection
                            connection.Shutdown(SocketShutdown.Both);
                            connection.Close();
                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine("Can't connect to Master.");
                            Logger.Log(this, ex.ToString(), silenced:true);

                            Thread.Sleep(10000);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(this, ex.ToString());

                            Thread.Sleep(10000);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log errors
                        Logger.Log(this, ex.ToString());
                        // Wait for 30 seconds
                        Thread.Sleep(30000);
                    }
                }
            }
        }
    }

    public void Render_SID_Temporal(string blend_file, int first_frame, int last_frame, string render_engine)
    {
        // Prepare Blender arguments
        string args = $"-b \"{blend_file}\" -t {Settings.Blender_Installations[0].CPU_Thread_Limit} -P SID_Temporal_Bridge.py -- {first_frame} {last_frame}";
        // Add render device for Cycles
        if (render_engine == "CYCLES")
        {
            args += " --cycles-device ";
            args += Settings.Blender_Installations[0].Render_Device;
        }

        // Use Blender to render project
        Process process = new Process();
        // Set Blender as executable
        process.StartInfo.FileName = Settings.Blender_Installations[0].Executable;
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
        Logger.Log(this, cmd_output);
    }

    public void Render(string blend_file, int first_frame, int last_frame, string render_engine, string file_format)
    {
        // Prepare Blender arguments
        string args = $"-b \"{blend_file}\" -o {Path.Join(Project_Directory, "frame_######")} -F {file_format} -s {first_frame} -e {last_frame} -t {Settings.Blender_Installations[0].CPU_Thread_Limit} -a";
        // Add render device for Cycles
        if (render_engine == "CYCLES")
        {
            args += " --cycles-device ";
            args += Settings.Blender_Installations[0].Render_Device;
        }

        // Use Blender to render project
        Process process = new Process();
        // Set Blender as executable
        process.StartInfo.FileName = Settings.Blender_Installations[0].Executable;
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

    #region Setup
    public static void First_Time_Setup()
    {
        // Create empty settings object
        Settings = new ClientSettings();
        // Removes the need to type "new List<string> { "Yes", "No" }" every time
        List<string> basic_bool = new List<string> { "No", "Yes" };
        // user_input string
        string user_input;

        Settings.Database_Connection = new DBConnection(DBMode.SQLite, Database_Directory);
        Settings.Shutdown_Parameters = new AutoShutdown();

        // Enable logging
        // Use Menu() to grab user input
        Menu menu = new Menu
        (
            basic_bool,
            "Client",
            new List<string> { "Enable logging? (It is recommended to turn this on. To see whats included please refer to the documentation!)" }
        );
        Settings.Enable_Logging = Helpers.Parse_Bool(menu.Show());

        menu = new Menu
        (
            basic_bool,
            "Client",
            new List<string> { "Keep the files reiceved by the Master?",
                               "If this is disabled, the Client will redownload the project every time" }
        );
        Settings.Keep_Input = Helpers.Parse_Bool(menu.Show());

        menu = new Menu
        (
            basic_bool,
            "Client",
            new List<string> { "Keep the rendered files?",
                               "If this is disabled, the Client will delete all rendered files after sending them to the master" }
        );
        Settings.Keep_Output = Helpers.Parse_Bool(menu.Show());

        menu = new Menu
        (
            basic_bool,
            "Client",
            new List<string> { "Keep the ZIP files?",
                               "If this is disabled, the Client will delete all ZIP files after sending them to the master" }
        );
        Settings.Keep_ZIP = Helpers.Parse_Bool(menu.Show());

        // Add Master
        // Let the user input a valid IP adress with Port
        // E.g. "127.0.0.1:8080", "127.0.0.1:8081"
        Settings.Master_Connections = new List<MasterConnection>();
        Display.Show_Top_Bar("Client");
        Console.WriteLine("You now have the option to add multiple Master connections.");
        Console.WriteLine("You need to add at least one connection.");
        while (true)
        {
            Console.WriteLine("If you want to add a Master write it like this 'IPv4:Port', e.g. '127.0.0.1:19186'");

            user_input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(user_input) && Settings.Master_Connections.Count > 0)
            {
                break;
            }

            else if (!Helpers.Check_Split_IP_Port(user_input))
            {
                Console.WriteLine("Please input a valid combination of a IPv4 and a port");
                Console.WriteLine("If you are having trouble you can contact us on Discord at any time!");
            }

            else if (Helpers.Check_Split_IP_Port(user_input))
            {
                string[] split = user_input.Split(':');
                Settings.Master_Connections.Add(new MasterConnection(split[0], int.Parse(split[1])));

                Console.WriteLine("Would you like to add another Master? (leave empty for no; paste the connection string for yes)");
            }
        }
        Console.Clear();

        // Blender Executable
        // Check if the file exsists
        Settings.Blender_Installations = new List<Blender>();
        Display.Show_Top_Bar("Client");
        Console.WriteLine("You now have the option to add multiple Blender installations.");
        Console.WriteLine("You need to add at least one installation.");
        Console.WriteLine("");
        while (true)
        {
            Console.WriteLine("Where is your blender executable stored? (It's recommended NOT to use blender-launcher)");
            Console.WriteLine("Please input the path to your blender executable");
            user_input = Console.ReadLine().Replace("\"", "").Replace("'", "");

            if (string.IsNullOrWhiteSpace(user_input) && Settings.Blender_Installations.Count > 0)
            {
                break;
            }

            else if (File.Exists(user_input))
            {
                Settings.Blender_Installations.Add(new Blender
                {
                    Executable = user_input,
                });

                menu = new Menu
                (
                    new List<string> { "CPU", "CUDA", "OPTIX", "HIP", "ONEAPI", "METAL", "OPENCL" },
                    "Client",
                    new List<string> { "What device/API to use for rendering? (Be sure your device supports your selection!)" }
                );
                Settings.Blender_Installations.Last().Render_Device = menu.Show();

                // Add CPU
                if (Settings.Blender_Installations.Last().Render_Device != "CPU")
                {
                    menu = new Menu
                    (
                        basic_bool,
                        "Client",
                        new List<string> { "Enable Hybrid Cycles-rendering? (For some system configurations it cuts the render time, for some it increases it)" }
                    );

                    if (Helpers.Parse_Bool(menu.Show()))
                    {
                        Settings.Blender_Installations.Last().Render_Device += "+CPU";
                    }
                    else
                    {
                        Settings.Blender_Installations.Last().CPU_Thread_Limit = 2;
                    }
                }
                else
                {
                    Settings.Blender_Installations.Last().CPU_Thread_Limit = Environment.ProcessorCount;
                }

                // Select allowed render engines
                (Settings.Blender_Installations.Last().Allowed_Render_Engines, Settings.Blender_Installations.Last().Version) = Pick_Render_Engines(user_input, Settings.Enable_Logging);

                Display.Show_Top_Bar("Client");
                Console.WriteLine("Would you like to add another installation? (leave empty for no; paste the path for yes)");
            }
        }
        Console.Clear();

        // Data collection
        // Use Menu() to grab user input
        menu = new Menu
        (
            basic_bool,
            "Client",
            new List<string> { "Save debug data? (it is only stored locally)",
                               "You can find a list of all the data stored in the documentation" }
        );
        Settings.Allow_Data_Collection = Helpers.Parse_Bool(menu.Show());

        // Save the settings
        Settings_File_Handler.Save_Settings(Settings);

        Display.Show_Top_Bar("Client");
        Console.WriteLine("Setup complete!");
        Console.WriteLine("You can find more and experimental settings in the file 'client_settings.json'");
        Console.WriteLine("Press any key to continue.");

        Console.ReadKey();
    }
    public static (List<string>, string) Pick_Render_Engines(string blender_executable, bool enable_logging)
    {
        // Tell the user why it is taking so long
        Display.Show_Top_Bar("Client");
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
        List<string> lines = File.ReadLines(Path.Join(Bin_Directory, "engines.txt")).ToList();
        string version_string = lines[0];
        //string[] version_list = version_string.Split('.');
        //(int, int, int) version = (int.Parse(version_list[0]), int.Parse(version_list[1]), int.Parse(version_list[2]));
        string version = version_string;
        lines.Remove(version_string);
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
        Menu menu = new Menu
        (
            items,
            "Client",
            new List<string> { "" }
        );
        string selection = menu.Show();
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
                menu = new Menu
                (
                    engines,
                    "Client",
                    new List<string> { "Choose one by one which render engines to allow." }
                );
                selection = menu.Show();

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
    #endregion
}
