using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

using Libraries;
using Libraries.Models;
using Libraries.Models.Database;
using Libraries.Enums;

class Master
{
    #region Global Variables
    // Initialize global variables
    // Initialize global File names, directories
    public static string Bin_Directory = AppDomain.CurrentDomain.BaseDirectory;
    public static string Project_Directory = "";

    // Create global objects and variables
    public static MasterSettings Settings;
    public static Project Project;
    private static object Log_DB_Lock = new object();
    private static object Project_DB_Lock = new object();
    public static ProgressBar Progress_Bar;

    public static WebClient Web_Client;

    private static SettingsFileHandler Settings_File_Handler;

    private static bool Using_SRF = false;
    #endregion

    // Runs at start
    static void Main(string[] args)
    {
        Master master = new Master();
        master.Start(args);
    }
    public void Start(string[] args)
    {
        Settings_File_Handler = new SettingsFileHandler(Path.Join(Bin_Directory, "master_settings.json"));

        try
        {
            Settings = Settings_File_Handler.Load_Master_Settings(Path.Join(Bin_Directory, "master_settings_override.json"));
            if (string.IsNullOrWhiteSpace(Settings.Database_Connection.Path))
            {
                Settings.Database_Connection.Path = Path.Join(Bin_Directory, "Database");
            }
            File.Delete(Path.Join(Bin_Directory, "master_settings_override.json"));
        }
        catch (Exception)
        {
            try
            {
                Settings = Settings_File_Handler.Load_Master_Settings();
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
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected Exception, please contact Pidgeon Tools if the error persists!");
                Console.WriteLine(ex);
                Console.ReadLine();

                Environment.Exit(1);
            }
        }

        // Initialize static classes
        ProjectFileHandler.Bin_Directory = Bin_Directory;
        Logger.Initialize(Settings.Enable_Logging, Settings.Log_level);

        if (!Directory.Exists(Settings.Database_Connection.Path))
        {
            Directory.CreateDirectory(Settings.Database_Connection.Path);
        }
        DBHandler.Initialize(Settings.Database_Connection);

        new SystemInfo(Settings.Allow_Data_Collection, Bin_Directory);

        try
        {
            (Project, Project_Directory) = ProjectFileHandler.Load_Project(Path.Join(Bin_Directory, "startup_project.prfp"));
            Using_SRF = true;
            File.Delete(Path.Join(Bin_Directory, "startup_project.prfp"));
            Directory.CreateDirectory(Project_Directory);
            Initialize_Project();
            Render_Project();
        }
        catch (Exception ex)
        {
            Logger.Log(this, ex.ToString(), LogLevel.Warn, true);
        }

        if (!Using_SRF)
        {
            // If it fails, just continue to the main menu
            Main_Menu();
        }
    }
    
    void Main_Menu()
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

        Menu menu = new Menu(items, "Master");

        while (true)
        {
            // Show the Menu and grab the selection
            string selection = menu.Show();

            // Compare selection with options and execute function
            if (selection == items[0])
            {
                // Create a new project
                Project_Setup();
            }

            else if (selection == items[1])
            {
                // Let user input a valid PRF project file
                Display.Show_Top_Bar("Master");

                string user_input = "";
                while (!File.Exists(user_input) && !user_input.ToLower().EndsWith("prfp"))
                {
                    Console.WriteLine("Where is your PidgeonRenderFarm project stored?");
                    Console.WriteLine("Please input the path to your project");
                    user_input = Console.ReadLine().Replace("\"", "").Replace("'", "");
                }

                try
                {
                    // Load the project from the given file
                    (Project, Project_Directory) = ProjectFileHandler.Load_Project(user_input);
                    DBHandler.Initialize_Project_Table(Project.ID, true);
                    // Render project
                    Render_Project();
                }
                catch (FileLoadException)
                {
                    Logger.Log(this, $"Invalid project file: {user_input}", LogLevel.Error);
                }
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
                Environment.Exit(0);
            }
        }
    }

    void Render_Project()
    {
        Console.Clear();
        Progress_Bar = new ProgressBar(Project.Frames_Total, "Rendering: ");

        IPAddress ip_address;
        if (string.IsNullOrWhiteSpace(Settings.IPv4_Overwrite))
        {
            // Get the local IPv4 of device
            ip_address = Helpers.Get_IPv4();
        }
        else
        {
            ip_address = IPAddress.Parse(Settings.IPv4_Overwrite);
        }

        Logger.Log(this, "Master IPv4: " + ip_address.ToString(), silenced:true);
        
        Display.Show_Top_Bar("Master", new List<string> { $"Master IP address: {ip_address}",
                                                          $"Master Port: {Settings.Port}" });

        List<Thread> threads = new List<Thread>();

        Progress_Bar.Show();

        // Create a end point with given IP and port
        IPEndPoint local_end_point = new IPEndPoint(ip_address, Settings.Port);

        DateTime start_time = DateTime.Now;

        try
        {
            // Socket for clients initial connection (TCP)
            TcpListener listener = new(ip_address, Settings.Port);
            listener.Start();

            Logger.Log(this, "Waiting for Clients...");

            int frames_pending = Project.Frames_Total;

            // While there are unrendered frames await clients
            while (frames_pending > 0)
            {
                try
                {
                    if (threads.Count < Settings.Client_Limit || Settings.Client_Limit == 0)
                    {
                        if (listener.Pending())
                        {
                            Socket handler = listener.AcceptSocket();

                            Logger.Log(this, $"Client {handler.RemoteEndPoint} connected");

                            Thread thread = new Thread(() => Client_Handler(handler));
                            threads.Add(thread);
                            thread.Start();
                        }
                        else
                        {
                            Thread.Sleep(1000);
                        }
                    }
                    else
                    {
                        Logger.Log(this, $"Refused Client. Reason: too many Clients", LogLevel.Info);
                        Thread.Sleep(5000);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(this, ex.ToString(), LogLevel.Error);
                }

                lock (Project_DB_Lock)
                {
                    frames_pending = DBHandler.Select_Frames_Table_All_Pending().Count;
                    Logger.Log(this, "Frames pending: " + frames_pending);

                    Progress_Bar.Update(frames_pending, true);
                }
            }
            listener.Stop();
        }
        catch (Exception ex)
        {
            // Log errors
            Logger.Log(this, ex.ToString(), LogLevel.Fatal);
        }

        Logger.Log(this, $"Rendering finished! After {DateTime.Now - start_time}");

        if (Project.Use_SID_Temporal)
        {
            // Prepare Blender arguments
            string args = $"-b \"{Project.Full_Path_Blend}\" -P {Path.Join(Bin_Directory, "SID_Temporal_Bridge.py")}";

            // Use Blender to render project
            Process process = new Process();
            // Set Blender as executable
            process.StartInfo.FileName = Settings.Blender_Installations.FirstOrDefault(blender =>
                                blender.Version.Split('.')[0] == Project.Blender_Version.Split('.')[0]
                                && blender.Version.Split('.')[1] == Project.Blender_Version.Split('.')[1]).Executable;
            // Use the command string as args
            process.StartInfo.Arguments = args;
            process.StartInfo.CreateNoWindow = true;
            // Redirect output to log Blenders output
            process.StartInfo.RedirectStandardOutput = Settings.Enable_Logging;
            process.Start();
            // Print and log the output
            process.WaitForExit();
            string cmd_output = process.StandardOutput.ReadToEnd();
            Logger.Log(this, cmd_output, LogLevel.Debug, true);

            Logger.Log(this, $"Denoising finished! After {DateTime.Now - start_time}");
        }

        if (!Using_SRF)
        {
            Console.WriteLine("You can go back to the main menu by pressing any key");
            Console.ReadKey();
        }
        else
        {
            Environment.Exit(0);
        }
    }

    void Client_Handler(Socket client)
    {
        try
        {
            // Receive message from client
            byte[] buffer = new byte[8192];
            int received = client.Receive(buffer);
            // Convert bytes to string
            string json_receive = Encoding.UTF8.GetString(buffer, 0, received);
            ClientResponse client_response;

            Logger.Log(this, json_receive, LogLevel.Trace, true);

            // Convert string to an object
            client_response = JsonSerializer.Deserialize<ClientResponse>(json_receive);

            // Prepare a new response and string
            MasterResponse master_response = new MasterResponse();
            string json_send;

            // Find out what the client wants
            if (client_response.Message == "new")
            {
                // If Client wants work, check if frames are left
                List<Frame> frames = Aquire_Frames(client_response, client.RemoteEndPoint.ToString().Split(':')[0], out string reason);

                master_response.Message = "NAN";

                // If frames left, then provide Client with information about the project
                if (frames.Count > 0)
                {
                    master_response.Message = "here";
                    master_response.ID = Project.ID;
                    master_response.File_Size = new FileInfo(Project.Full_Path_Blend).Length;
                    master_response.Frames = frames;
                    master_response.Frame_Step = Project.Frame_Step;
                    master_response.Render_Engine = Project.Render_Engine;
                    master_response.File_Format = Project.Output_File_Format;
                    master_response.Use_SID_Temporal = Project.Use_SID_Temporal;

                    if (Project.File_transfer_Mode == FileTransferMode.SMB)
                    {
                        master_response.File_transfer_Mode = FileTransferMode.SMB;

                        if (client_response.Is_Windows)
                        {
                            master_response.Connection_String = Settings.SMB_Connection.Get_Windows_String();
                        }
                        else
                        {
                            master_response.Connection_String = Settings.SMB_Connection.Get_Unix_String();
                        }
                    }
                    else if (Project.File_transfer_Mode == FileTransferMode.FTP)
                    {
                        master_response.File_transfer_Mode = FileTransferMode.FTP;

                        master_response.Connection_String = Settings.FTP_Connection.Connection_String;
                    }
                }

                // Convert the object to string
                json_send = JsonSerializer.Serialize(master_response);
                Logger.Log(this, json_send, LogLevel.Trace, true);
                // Convert string to bytes
                byte[] bytes_send = Encoding.UTF8.GetBytes(json_send);
                // Send bytes to client
                client.Send(bytes_send);

                if (master_response.Message == "NAN")
                {
                    Logger.Log(this, $"No frames assigned to {client.RemoteEndPoint}. Reason: {reason}");
                }
                else
                {
                    Logger.Log(this, $"Frame {frames.First().Id} - {frames.Last().Id} assigned to {client.RemoteEndPoint}");

                    if (Project.File_transfer_Mode == FileTransferMode.TCP)
                    {
                        // Receive message from client
                        buffer = new byte[1024];
                        received = client.Receive(buffer);
                        // Convert bytes to string
                        json_receive = Encoding.UTF8.GetString(buffer, 0, received);
                        Logger.Log(this, json_receive, LogLevel.Trace, true);
                        // Convert string to an object
                        client_response = JsonSerializer.Deserialize<ClientResponse>(json_receive);

                        // If the client doesn't have the .Blend, send it to him
                        if (client_response.Message == "needed")
                        {
                            client.SendFile(Project.Full_Path_Blend);
                        }
                    }
                }
            }

            else if (client_response.Message == "output")
            {
                // If Client is done rendering execute this
                // Check if Clients work is any good
                // If it isn't add his frames back
                List<Frame> faulty_frames = client_response.Frames.Where(frame => frame.Quality == false).ToList();

                if (faulty_frames.Count > 0)
                {
                    lock (Project_DB_Lock)
                    {
                        DBHandler.Update_Frames_Table(faulty_frames);
                    }

                    client_response.Frames.RemoveAll(frame => faulty_frames.Contains(frame));
                }

                // Generate a file name
                string zip_file = client_response.Frames.First().Id + "_" + client_response.Frames.Last().Id + ".zip";

                lock (Project_DB_Lock)
                {
                    // If not all frames are faulty
                    if (faulty_frames.Count != client_response.Frames.Count &&
                    Project.File_transfer_Mode == FileTransferMode.TCP)
                    {
                        // Send a message to the Client to syncronize
                        byte[] bytes_send = Encoding.UTF8.GetBytes("drop");
                        client.Send(bytes_send);
                    
                        // Generate a path for the file
                        string path = Path.Join(Project_Directory, zip_file);
                        // Download the file into the path
                        using (FileStream file_stream = File.Create(path))
                        {
                            new NetworkStream(client).CopyTo(file_stream);
                        }

                        Logger.Log(this, $"Frame {client_response.Frames.First().Id} - {client_response.Frames.Last().Id} downloaded from {client.RemoteEndPoint}");
                    }

                    else if (faulty_frames.Count != client_response.Frames.Count &&
                        Project.File_transfer_Mode == FileTransferMode.SMB)
                    {
                        string remote_path = Path.Join(Settings.SMB_Connection.Connection_String, Project_Directory, zip_file);
                        if (Project.Download_Remote_Input)
                        {
                            if (File.Exists(remote_path))
                            {
                                File.Copy(remote_path, Path.Join(Project_Directory, zip_file));
                            }
                            else
                            {
                                Logger.Log(this, "ZIP file does not exist");
                            }
                        }
                    }

                    else if (faulty_frames.Count != client_response.Frames.Count &&
                        Project.File_transfer_Mode == FileTransferMode.FTP)
                    {
                        string remote_path = Path.Join(Settings.FTP_Connection.Connection_String, Project_Directory, zip_file);

                        try
                        {
                            Web_Client.DownloadFile(remote_path, Path.Join(Project_Directory, zip_file));
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(this, $"File does not exist on remote directory. Exception: {ex}", LogLevel.Error);
                        }
                    }
                
                    Verify_Frames(client_response.Frames, zip_file, Project.ID);
                }
                
            }

            else if (client_response.Message == "ping")
            {
                // If Client is pinging master
                // Return "pong"
                master_response.Message = "pong";
                // Convert the object to string
                json_send = JsonSerializer.Serialize(master_response);
                // Convert string to bytes
                byte[] bytes_send = Encoding.UTF8.GetBytes(json_send);

                Logger.Log(this, "Received ping!");

                client.Send(bytes_send);
            }

            // Cut the connection
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        catch (Exception ex)
        {
            Logger.Log(this, ex.ToString(), LogLevel.Error);
        }
    }

    void Verify_Frames(List<Frame> frames, string file, string project_ID)
    {
        List<Frame> valid_frames = new List<Frame>();
        List<Frame> faulty_frames = new List<Frame>();

        string base_directory = Bin_Directory;

        if (Project.File_transfer_Mode == FileTransferMode.SMB &&
            !Project.Download_Remote_Input)
        {
            base_directory = Settings.SMB_Connection.Connection_String;
        }

        base_directory = Path.Join(base_directory, project_ID);
        string zip_path = Path.Join(base_directory, file);

        // Extract the contents
        if (File.Exists(zip_path))
        {
            ZipFile.ExtractToDirectory(zip_path, base_directory);
        }
        else
        {
            // Check for every frame
            foreach (Frame frame in frames)
            {
                faulty_frames.Add(new Frame
                {
                    Id = frame.Id,
                    State = "Open"
                });
            }

            lock (Project_DB_Lock)
            {
                DBHandler.Update_Frames_Table(faulty_frames);
            }

            return;
        }

        if (Project.Use_SID_Temporal)
        {
            if (Directory.Exists(Path.Join(base_directory, "noisy")))
            {
                base_directory = Path.Join(base_directory, "noisy");
            }
            else if (Directory.Exists(Path.Join(base_directory, "processing")))
            {
                base_directory = Path.Join(base_directory, "processing");
            }
            else
            {
                foreach (Frame frame in frames)
                {
                    faulty_frames.Add(new Frame
                    {
                        Id = frame.Id,
                        State = "Open"
                    });
                }

                lock (Project_DB_Lock)
                {
                    DBHandler.Update_Frames_Table(faulty_frames);
                }

                return;
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

        // Check for every frame
        foreach (Frame frame in frames)
        {
            // If the file exsists add it to list
            if (File.Exists(Path.Join(base_directory, frame.File_Name)))
            {
                // Index for every file and the frame is the same,
                // so use position of file for the frame
                valid_frames.Add(new Frame
                {
                    Id = frame.Id,
                    State = "Rendered"
                });
            }

            else
            {
                faulty_frames.Add(new Frame
                {
                    Id = frame.Id,
                    State = "Open"
                }); ;
            }
        }
        
        DBHandler.Update_Frames_Table(valid_frames);
        DBHandler.Update_Frames_Table(faulty_frames);

        Logger.Log(this, $"Frame {frames.First().Id} - {frames.Last().Id} rendered!");
    }

    // Aquire frames for the Client
    List<Frame> Aquire_Frames(ClientResponse requirements, string ipv4, out string reason)
    {
        List<Frame> old_frames = new List<Frame>();
        lock (Project_DB_Lock)
        {
            old_frames = DBHandler.Select_Assigned_Frames_Table(ipv4);
        }
        
        if (old_frames.Count > 0)
        {
            foreach (Frame frame in old_frames)
            {
                frame.State = "Open";
            }

            DBHandler.Update_Frames_Table(old_frames);
        }

        // Have an empty list ready
        List<Frame> empty_list = new List<Frame>{};
        // If Client Blender version does not match
        // Don't give him frames
        List<Blender> tmp = requirements.Blender_Installations.Where(blender =>
                                blender.Version.Split('.')[0] == Project.Blender_Version.Split('.')[0]
                                && blender.Version.Split('.')[1] == Project.Blender_Version.Split('.')[1]).ToList();

        if (tmp.Count == 0)
        {
            reason = "Blender version missmatch";
            return empty_list;
        }

        // If Client is not allowed to use engine
        // Don't give him frames
        bool has_render_engine = false;

        foreach (Blender blender in tmp)
        {
            if (blender.Allowed_Render_Engines.Contains(Project.Render_Engine)
                || blender.Allowed_Render_Engines.Contains("other"))
            {
                has_render_engine = true;
            }
        }

        if (!has_render_engine)
        {
            reason = "Render engine not installed";
            return empty_list;
        }

        // If Client doesn't allow for as much time
        // Don't give him frames
        if (requirements.Render_Time_Limit != 0
            && requirements.Render_Time_Limit < Project.Time_Per_Frame)
        {
            reason = "Time limit exceeded";
            return empty_list;
        }

        // If Client doesn't want to use as much RAM
        // Don't give him frames
        if (requirements.RAM_Use_Limit != 0
            && requirements.RAM_Use_Limit < Project.RAM_Use)
        {
            reason = "RAM limit exceeded";
            return empty_list;
        }

        // Make sure to only change a thing at once
        lock (Project_DB_Lock)
        {
            List<Frame> frames = DBHandler.Select_Frames_Table(Project.Batch_Size);

            if (frames.Count == 0)
            {
                reason = "No open frames";
                return empty_list;
            }

            // Create a list with frames for Client
            List<Frame> frames_picked = new List<Frame>();

            int next_frame = frames[0].Id;
            foreach (Frame frame in frames)
            {
                if (frame.Id == next_frame)
                {
                    frames_picked.Add(new Frame
                    {
                        Id = frame.Id,
                        State = "Rendering",
                        IPv4 = ipv4
                    });
                    next_frame = frame.Id + 1;
                }
                else
                {
                    break;
                }
            }

            DBHandler.Update_Frames_Table(frames_picked);

            reason = "";
            return frames_picked;
        }
    }

    #region Setup
    void First_Time_Setup()
    {
        // Create empty settings object
        Settings = new MasterSettings();
        // Removes the need to type "new List<string> { "Yes", "No" }" every time
        List<string> basic_bool = new List<string> { "No", "Yes" };
        string user_input = "";

        Settings.Database_Connection = new DBConnection(DBMode.SQLite, Path.Join(Bin_Directory, "Database"));
        Settings.SMB_Connection = new SMBConnection("", "", "");
        Settings.FTP_Connection = new FTPConnection("", "", "");
        Settings.Log_level = LogLevel.Info;

        // Enable logging
        // Use Menu() to grab user input
        Menu menu = new Menu
        (
            basic_bool,
            "Master",
            new List<string> { "Enable logging? (It is recommended to turn this on.)" }
        );
        Settings.Enable_Logging = Helpers.Parse_Bool(menu.Show());

        // Port
        // Let the user input a valid port
        // If emtpy, then use default port
        Display.Show_Top_Bar("Master");
        while (!Helpers.Is_Port(user_input))
        {
            Console.WriteLine("Which Port to use? (Default: 19186):");
            Console.WriteLine("Please input a whole number between 1 and 65535 (or leave empty for the default value)");
            user_input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(user_input))
            {
                user_input = "19186";
            }
        }
        Settings.Port = int.Parse(user_input);
        Console.Clear();

        // Blender Executable
        // Check if the file exsists
        Settings.Blender_Installations = new List<Blender>();
        Display.Show_Top_Bar("Master");
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
                // Prepare Blender arguments
                string args = $"-b -P {Path.Join(Bin_Directory, "Get_Version.py")}";

                // Use Blender to obtain the render engines and Blender version
                Process process = new Process();
                // Set Blender as executable
                process.StartInfo.FileName = user_input;
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
                List<string> lines = File.ReadLines(Path.Join(Path.GetDirectoryName(user_input), "version.txt")).ToList();
                string version_string = lines[0];

                Settings.Blender_Installations.Add(new Blender
                {
                    Executable = user_input,
                    Version = version_string,
                });

                Console.WriteLine("Would you like to add another installation? (leave empty for no; paste the path for yes)");
            }
        }
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

        // Allow computation
        // Use Menu() to grab user input
        menu = new Menu
        (
            basic_bool,
            "Master",
            new List<string> { "Allow computation on the master?" }
        );
        Settings.Allow_Computation = Helpers.Parse_Bool(menu.Show());

        // Data collection
        // Use Menu() to grab user input
        menu = new Menu
        (
            basic_bool,
            "Master",
            new List<string> { "Save debug data? (it is only stored locally)",
                               "You can find a list of all the data stored in the documentation" }
        );
        Settings.Allow_Data_Collection = Helpers.Parse_Bool(menu.Show());

        Logger.Initialize(Settings.Enable_Logging, Settings.Log_level);

        // Save the settings
        Settings_File_Handler.Save_Settings(Settings);

        Display.Show_Top_Bar("Master");
        Console.WriteLine("Setup complete!");
        Console.WriteLine("You can find more and experimental settings in the file 'master_settings.json'");
        Console.WriteLine("Press any key to continue.");

        Console.ReadKey();
    }
    #endregion

    #region Project_Setup
    // Run the setup for the project
    void Project_Setup()
    {
        // Create empty project
        Project = new Project();

        List<string> basic_bool = new List<string> { "No", "Yes" };
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        // Credits: https://stackoverflow.com/users/76217/dtb
        Random random = new Random();
        Project.ID = new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());

        // Blend file
        // Let user input a valid Blender project file
        Display.Show_Top_Bar("Master");
        Console.WriteLine("Where is your Blender project stored?");
        string user_input = "";
        while (!File.Exists(user_input) && (!user_input.ToLower().EndsWith(".blend") || !user_input.ToLower().EndsWith(".blend1")))
        {
            Console.WriteLine("Please input the path to your .blend");
            user_input = Console.ReadLine().Replace("\"", "").Replace("'", "");
        }
        Project.Full_Path_Blend = user_input;

        // Use SuperFastRender
        // Use Menu() to grab user input
        Menu menu = new Menu
        (
            basic_bool,
            "Master",
            new List<string> { "Use SuperFastRender (with default settings) to optimize the rendering process?" }
        );
        Project.Use_SFR = Helpers.Parse_Bool(menu.Show());

        // Use SuperImageDenoiser Temporal
        // Use Menu() to grab user input
        menu = new Menu
        (
            basic_bool,
            "Master",
            new List<string> { "Use SuperImageDenoiser Temporal (with default settings) for denoising? You will lose 1 frame at the start and 2 frames at the end of your animation." }
        );
        Project.Use_SID_Temporal = Helpers.Parse_Bool(menu.Show());

        // Render test frame (for time)
        // Use Menu() to grab user input
        menu = new Menu
        (
            basic_bool,
            "Master",
            new List<string> { "Render a test frame? (Will take some time)",
                               "Used for Client time limit and analytics." }
        );
        Project.Render_Test_Frame = Helpers.Parse_Bool(menu.Show());

        // Batch size
        // Let user input a valid size
        Display.Show_Top_Bar("Master");
        user_input = "";
        while (!int.TryParse(user_input, out _))
        {
            Console.WriteLine("Batch size:");
            Console.WriteLine("Please input a whole number");
            user_input = Console.ReadLine();
        }
        Project.Batch_Size = Math.Abs(int.Parse(user_input));
        Console.Clear();

        Project.Video_Generate = false;

        // Only required if we generate a video
        /*if (Project.Video_Generate)
        {
            // Video FPS
            // Let user input a valid value
            Display.Show_Top_Bar("Master");
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
            Project.Video_FPS = Math.Abs(int.Parse(user_input));
            Console.Clear();

            // Video rate control type
            // Use Menu() to grab user input
            Project.Video_Rate_Control = Menu(new List<string> { "CRF", "CBR" },
                                                  new List<string> { "What Video Rate Control to use?" });

            // Video rate control value (e.g. bitrate)
            // Let user input a valid value
            Display.Show_Top_Bar("Master");
            Console.WriteLine("Video Rate Control Value: (CRF - lower is better; CBR - higher is better)");
            user_input = Console.ReadLine();
            while (!int.TryParse(user_input, out _))
            {
                Console.WriteLine("Please input a whole number");
                user_input = Console.ReadLine();
            }
            Project.Video_Rate_Control_Value = Math.Abs(int.Parse(user_input));
            Console.Clear();

            // Resize the video
            // Use Menu() to grab user input
            Project.Video_Resize = Helpers.Parse_Bool(Menu(new List<string> { "Yes", "No" },
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
        }*/

        // Blender version
        // Use Menu() to grab user input
        List<string> options = new List<string>();

        foreach (Blender installation in Settings.Blender_Installations)
        {
            options.Add(installation.Version);
        }

        menu = new Menu
        (
            options,
            "Master",
            new List<string> { "Please select your prefered Blender version" }
        );
        string selection = menu.Show();
        Project.Blender_Version = selection;

        // Get project directory name and create it
        Project_Directory = Path.Join(Bin_Directory, Project.ID);
        Directory.CreateDirectory(Project_Directory);

        Display.Show_Top_Bar("Master");
        Console.WriteLine("Gathering informations of your project. This may take a while. Please wait...");

        try
        {
            // Create a command for blender to optain some variables
            string args = $"-b \"{Project.Full_Path_Blend}\" -P {Path.Join(Bin_Directory, "BPY.py")} -- {Helpers.Bool_To_Int(Project.Use_SFR)} {Helpers.Bool_To_Int(Project.Render_Test_Frame)}";

            // Use Blender to obtain informations about the project
            // additionally use SFR if selected
            Process process = new Process();
            // Set Blender as executable
            process.StartInfo.FileName = Settings.Blender_Installations.FirstOrDefault(blender => blender.Version == Project.Blender_Version).Executable;
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
            string json_string = File.ReadAllText(Path.Join(Path.GetDirectoryName(Project.Full_Path_Blend), "vars.json"));
            Logger.Log(this, json_string, LogLevel.Trace);
            ProjectInfo project_info = JsonSerializer.Deserialize<ProjectInfo>(json_string);
            File.Delete(Path.Join(Path.GetDirectoryName(Project.Full_Path_Blend), "vars.json"));

            // Apply the values to project object
            Project.Render_Engine = project_info.Render_Engine;
            Project.Time_Per_Frame = project_info.Render_Time;
            Project.Output_File_Format = project_info.File_Format;
            Project.First_Frame = project_info.First_Frame;
            Project.Last_Frame = project_info.Last_Frame;
            Project.Frame_Step = project_info.Frame_Step;

            Initialize_Project();

            // Save the project
            ProjectFileHandler.Save_Project(Project, Path.Join(Project_Directory, $"{Project.ID}.prfp"));

            // Start rendering
            Render_Project();
        }
        catch (Exception ex)
        {
            Logger.Log(this, ex.ToString(), LogLevel.Fatal);
        }
    }

    public static void Initialize_Project()
    {
        lock (Project_DB_Lock)
        {
            DBHandler.Initialize_Project_Table(Project.ID);

            // Append every frame to frames_left
            Project.Frames_Total = DBHandler.Insert_Frames_Table(Project.First_Frame, Project.Last_Frame, Project.Frame_Step);

            // If the file exsists add it to list
            string file = $"frame_{Project.First_Frame.ToString().PadLeft(6, '0')}.{Project.Output_File_Format}";
            string path = Path.Join(Path.GetDirectoryName(Project.Full_Path_Blend), file);
            if (File.Exists(path))
            {
                File.Move(path, Path.Join(Project_Directory, file));

                DBHandler.Update_Frames_Table(new List<Frame>
                {
                    new Frame
                    {
                        Id = Project.First_Frame,
                        State = "Rendered"
                    }
                });
            }

            #region Prepare project on remote directory
            // Make sure we do have a connection string
            if (string.IsNullOrWhiteSpace(Settings.SMB_Connection?.URL))
            {
                Project.File_transfer_Mode = FileTransferMode.TCP;
            }
            else if (string.IsNullOrWhiteSpace(Settings.FTP_Connection?.URL))
            {
                Project.File_transfer_Mode = FileTransferMode.TCP;
            }

            if (Project.File_transfer_Mode == FileTransferMode.SMB)
            {
                try
                {
                    File.Copy(Project.Full_Path_Blend,
                              Path.Join(Settings.SMB_Connection.Connection_String, Project.Full_Path_Blend));
                }
                catch (Exception ex)
                {
                    Project.File_transfer_Mode = FileTransferMode.TCP;
                }
            }
            else if (Project.File_transfer_Mode == FileTransferMode.FTP)
            {
                try
                {
                    Web_Client = new WebClient();
                    //Web_Client.Credentials = new NetworkCredential(Settings.FTP_Connection.User, Settings.FTP_Connection.Password);
                    Web_Client.UploadFile(Path.Join(Settings.FTP_Connection.Connection_String, Project.Full_Path_Blend),
                                          WebRequestMethods.Ftp.UploadFile,
                                          Project.Full_Path_Blend);
                }
                catch (Exception ex)
                {
                    Project.File_transfer_Mode = FileTransferMode.TCP;
                }
            }
            #endregion
        }
    }
    #endregion
}