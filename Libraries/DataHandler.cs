using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Data;

using Libraries.Models;
using Libraries.Models.Database;
using Libraries.Enums;

namespace Libraries;

/// <summary>
/// This class provides various functions for working with the internal databases
/// </summary>
public static class DBHandler
{
    #region Properties
    private static DBConnection DB;
    private static string Project_ID;

    private static SqliteConnection Log_SQLite_Connection;
    private static SqliteConnection Project_SQLite_Connection;
    #endregion

    /// <summary>
    /// This function initializes the database connection
    /// </summary>
    /// <param name="conn">The DBConnection specified in the settings</param>
    public static void Initialize(DBConnection conn)
    {
        DB = conn;

        Initialize_Log_Table();
    }

    /// <summary>
    /// Initialize the log database and table
    /// </summary>
    private static void Initialize_Log_Table()
    {
        if (DB.Mode == DBMode.SQLite)
        {
            if (!File.Exists(Path.Join(DB.Path, "Log.db")))
            {
                File.Create(Path.Join(DB.Path, "Log.db")).Dispose();
            }
            Log_SQLite_Connection = new SqliteConnection($"Data Source={Path.Join(DB.Path, "Log.db")}; Cache=Shared");
            Log_SQLite_Connection.Open();

            List<string> log_table = new List<string>();
            SqliteCommand query = Log_SQLite_Connection.CreateCommand();
            query.CommandText =
            "SELECT name " +
            "FROM sqlite_master " +
            "WHERE type=\"table\" " +
            "AND name=\"LOG\";";

            SqliteDataReader data_reader = query.ExecuteReader();
            while (data_reader.Read())
            {
                log_table.Add(data_reader.GetString(0));
            }

            if (log_table.Count == 0)
            {
                using (SqliteTransaction transaction = Log_SQLite_Connection.BeginTransaction())
                {
                    query = Log_SQLite_Connection.CreateCommand();
                    query.CommandText =
                    "CREATE TABLE \"LOG\" " +
                    "(\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, " +
                    "\"TIME\" TEXT NOT NULL, " +
                    "\"LEVEL\" TEXT NOT NULL, " +
                    "\"MODULE\" TEXT NOT NULL, " +
                    "\"MESSAGE\" TEXT NOT NULL)";
                    query.ExecuteNonQuery();

                    transaction.Commit();
                }
            }
        }
    }

    /// <summary>
    /// Initialize the project database with the table for the current project
    /// </summary>
    /// <param name="project_id">The ID of the current project</param>
    /// <param name="load">Wether the project is being loaded or created</param>
    public static void Initialize_Project_Table(string project_id, bool load = false)
    {
        Project_ID = project_id;

        if (DB.Mode == DBMode.SQLite)
        {
            if (!File.Exists(Path.Join(DB.Path, "Project.db")))
            {
                File.Create(Path.Join(DB.Path, "Project.db")).Dispose();
            }
            Project_SQLite_Connection = new SqliteConnection($"Data Source={Path.Join(DB.Path, "Project.db")}; Cache=Shared");
            Project_SQLite_Connection.Open();

            List<string> project_table = new List<string>();
            SqliteCommand query = Project_SQLite_Connection.CreateCommand();
            query.CommandText =
            "SELECT name " +
            "FROM sqlite_master " +
            "WHERE type=\"table\" " +
            $"AND name=\"{Project_ID}\";";

            SqliteDataReader data_reader = query.ExecuteReader();
            while (data_reader.Read())
            {
                project_table.Add(data_reader.GetString(0));
            }

            if (project_table.Count == 0)
            {
                if (!load)
                {
                    using (SqliteTransaction transaction = Project_SQLite_Connection.BeginTransaction())
                    {
                        query = Project_SQLite_Connection.CreateCommand();
                        query.CommandText =
                        $"CREATE TABLE \"{Project_ID}\" " +
                        "(\"ID\" INTEGER PRIMARY KEY NOT NULL, " +
                        "\"STATE\" TEXT NOT NULL," +
                        "\"Client\" TEXT)";
                        query.ExecuteNonQuery();

                        transaction.Commit();
                    }
                }
                else
                {
                    throw new DataException("Project not found in DB");
                }
            }
        }
    }

    #region Table: PROJECT_FRAMES
    /// <summary>
    /// Insert all frames of the project into the database based off the first and last frame, respecting the frame steps
    /// </summary>
    /// <param name="first_frame">The first frame of the animation</param>
    /// <param name="last_frame">The last frame of the animation</param>
    /// <param name="frame_step">how many frames are skipped between frames (1 = no frame is skipped)</param>
    /// <returns>Returns the total amount of frames to be rendered</returns>
    public static int Insert_Frames_Table(int first_frame, int last_frame, int frame_step = 1)
    {
        int frames_total = 0;
        if (DB.Mode == DBMode.SQLite)
        {
            using (SqliteTransaction transaction = Project_SQLite_Connection.BeginTransaction())
            {
                for (int i = first_frame; i <= last_frame; i += frame_step)
                {
                    SqliteCommand query = Project_SQLite_Connection.CreateCommand();
                    query.CommandText =
                    $"INSERT INTO \"{Project_ID}\" " +
                    "(ID, STATE) " +
                    "VALUES " +
                    $"({i}, \"Open\");";

                    //Console.WriteLine(query.CommandText);
                    query.ExecuteNonQuery();

                    transaction.Commit();

                    frames_total++;
                }
            }
        }

        return frames_total;
    }

    /// <summary>
    /// Select all frames that are not done rendering or currently rendering
    /// </summary>
    /// <returns>Returns the number of each frame in a list</returns>
    public static List<int> Select_Frames_Table_All_Open()
    {
        List<int> frames = new List<int>();

        if (DB.Mode == DBMode.SQLite)
        {
            Console.WriteLine(DB.Path);

            SqliteCommand query = Project_SQLite_Connection.CreateCommand();
            query.CommandText =
            "SELECT ID " +
            $"FROM \"{Project_ID}\" " +
            $"WHERE STATE = \"Open\"";

            SqliteDataReader data_reader = query.ExecuteReader();
            while (data_reader.Read())
            {
                frames.Add(data_reader.GetInt32(0));
            }
        }

        return frames;
    }

    /// <summary>
    /// Select all pending frames - all frames currently rendering or not rendering
    /// </summary>
    /// <returns>Returns the number of each frame in a list</returns>
    public static List<int> Select_Frames_Table_All_Pending()
    {
        List<int> frames = new List<int>();

        if (DB.Mode == DBMode.SQLite)
        {
            SqliteCommand query = Project_SQLite_Connection.CreateCommand();
            query.CommandText =
            "SELECT ID " +
            $"FROM \"{Project_ID}\" " +
            $"WHERE STATE = \"Open\" " +
            $"OR STATE = \"Rendering\"";

            SqliteDataReader data_reader = query.ExecuteReader();
            while (data_reader.Read())
            {
                frames.Add(data_reader.GetInt32(0));
            }
        }

        return frames;
    }

    /// <summary>
    /// Select all frames that are not currently rendering or done rendering, limit the amount to the batch size
    /// </summary>
    /// <param name="batch_size">Batch size specified in the project setting.</param>
    /// <returns>Returns a list of Frame</returns>
    public static List<Frame> Select_Frames_Table(int batch_size)
    {
        List<Frame> frames = new List<Frame>();

        if (DB.Mode == DBMode.SQLite)
        {
            SqliteCommand query = Project_SQLite_Connection.CreateCommand();
            query.CommandText =
            "SELECT ID " +
            $"FROM \"{Project_ID}\" " +
            $"WHERE STATE = \"Open\" " +
            $"LIMIT {batch_size}";

            SqliteDataReader data_reader = query.ExecuteReader();
            while (data_reader.Read())
            {
                frames.Add(new Frame
                {
                    Id = data_reader.GetInt32(0)
                });
            }
        }

        return frames;
    }

    /// <summary>
    /// Select all frames assigned to a specific client
    /// </summary>
    /// <param name="ipv4">The IPv4 of the client</param>
    /// <returns>Returns a list of Frame</returns>
    public static List<Frame> Select_Assigned_Frames_Table(string ipv4)
    {
        List<Frame> frames = new List<Frame>();

        if (DB.Mode == DBMode.SQLite)
        {
            SqliteCommand query = Project_SQLite_Connection.CreateCommand();
            query.CommandText =
            "SELECT ID " +
            $"FROM \"{Project_ID}\" " +
            "WHERE STATE = \"Rendering\" " +
            $"AND Client = \"{ipv4}\"";

            SqliteDataReader data_reader = query.ExecuteReader();
            while (data_reader.Read())
            {
                frames.Add(new Frame
                {
                    Id = data_reader.GetInt32(0)
                });
            }
        }

        return frames;
    }

    /// <summary>
    /// Update all frames to the given IPv4 and state
    /// </summary>
    /// <param name="frames">List of all frames providing the state and IPv4</param>
    public static void Update_Frames_Table(List<Frame> frames)
    {
        if (DB.Mode == DBMode.SQLite)
        {
            using (SqliteTransaction transaction = Project_SQLite_Connection.BeginTransaction())
            {
                foreach (Frame frame in frames)
                {
                    SqliteCommand query = Project_SQLite_Connection.CreateCommand();
                    query.CommandText =
                    $"UPDATE \"{Project_ID}\" " +
                    $"SET STATE = \"{frame.State}\" " +
                    (string.IsNullOrEmpty(frame.IPv4) ? "" : $", Client = \"{frame.IPv4}\" ") +
                    $"WHERE ID = {frame.Id}";
                    query.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }
    }
    #endregion

    #region Table: LOG
    /// <summary>
    /// Insert a LogEntry into the database
    /// </summary>
    /// <param name="time">The time of the occurrence</param>
    /// <param name="level">The level of the event</param>
    /// <param name="module">Where the event occurred</param>
    /// <param name="message">The message of the event</param>
    public static void Insert_Log_Table(string time, LogLevel level, string module, string message)
    {
        if (DB.Mode == DBMode.SQLite)
        {
            using (SqliteTransaction transaction = Log_SQLite_Connection.BeginTransaction())
            {
                SqliteCommand query = Log_SQLite_Connection.CreateCommand();
                query.CommandText =
                "INSERT INTO LOG " +
                "(TIME, LEVEL, MODULE, MESSAGE) " +
                "VALUES " +
                $"(\"{time}\", \"{level.ToString()}\", \"{module}\", \"{message}\");";
                query.ExecuteNonQuery();

                transaction.Commit();
            }
        }
    }
    #endregion

    /// <summary>
    /// Cut the connections to the databases
    /// </summary>
    public static void Shutdown_Database()
    {
        if (DB.Mode == DBMode.SQLite)
        {
            Project_SQLite_Connection.Close();
            Log_SQLite_Connection.Close();
        }
    }
}

/// <summary>
/// This class provides various functions for handling files
/// </summary>
public class FileHandler
{
    /// <summary>
    /// Save the SystemInfo to a json file
    /// </summary>
    /// <param name="file_path">Where to save the file</param>
    /// <param name="info">The SystemInfo object</param>
    public static void Save_SystemInfo(string file_path, SystemInfo info)
    {
        // Convert object to json
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        string settings_json = JsonSerializer.Serialize(info, options);

        // Write to file
        File.WriteAllText(file_path, settings_json);
    }

    /// <summary>
    /// Read all contents of a file, raise an exception if the file doesn't exist
    /// </summary>
    /// <param name="file_path">The path to the file</param>
    /// <returns>Returns the file contents</returns>
    public static string Read_File_Contents(string file_path)
    {
        // Check if settings exsist, else run setup
        if (!File.Exists(file_path))
        {
            throw new FileNotFoundException("File missing!");
        }

        // Load string from file and convert it to object
        // Update global settings object
        string file_contents = File.ReadAllText(file_path);

        return file_contents;
    }
}

/// <summary>
/// This class provides various functions for handling project files
/// </summary>
public static class ProjectFileHandler
{
    public static string Bin_Directory;

    /// <summary>
    /// Save to project to a file on the disk
    /// </summary>
    /// <param name="project">The project object from the memory</param>
    /// <param name="file_path">Where to save the file</param>
    public static void Save_Project(Project project, string file_path)
    {
        // Convert object to json
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        string settings_json = JsonSerializer.Serialize(project, options);

        // Write to file
        File.WriteAllText(file_path, settings_json);
    }

    /// <summary>
    /// Load a project from a file
    /// </summary>
    /// <param name="file_path">The path to the file</param>
    /// <returns>Returns the project object and the project directory</returns>
    public static (Project, string) Load_Project(string file_path)
    {
        string json_string;

        try
        {
            json_string = FileHandler.Read_File_Contents(file_path);
        }
        catch (FileNotFoundException)
        {
            throw new FileNotFoundException("Project file missing!");
        }

        try
        {
            return Load_Project_From_String(json_string);
        }
        catch (FileLoadException)
        {
            throw new FileNotFoundException("Project file contains unknown contents!");
        }
    }

    /// <summary>
    /// Loads a project from a given json string
    /// </summary>
    /// <param name="json_string">The json string</param>
    /// <returns>Returns the project object and the project directory</returns>
    public static (Project, string) Load_Project_From_String(string json_string)
    {
        Project project;

        try
        {
            project = JsonSerializer.Deserialize<Project>(json_string);
        }
        catch
        {
            throw new FileLoadException("Project file contains unknown contents!");
        }

        string project_directory = Path.Join(Bin_Directory, project.ID);

        return (project, project_directory);
    }
}

/// <summary>
/// This class provides various functions for handling settings files
/// </summary>
public class SettingsFileHandler
{
    private string File_Path;

    /// <summary>
    /// Initialize the class as object
    /// </summary>
    /// <param name="file_path"></param>
    public SettingsFileHandler(string file_path)
    {
        File_Path = file_path;
    }

    /// <summary>
    /// Save the Client settings to a file on the disk
    /// </summary>
    /// <param name="settings">The ClientSettings object</param>
    public void Save_Settings(ClientSettings settings)
    {
        // Convert object to json
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        string settings_json = JsonSerializer.Serialize(settings, options);

        // Write to file
        File.WriteAllText(File_Path, settings_json);
    }

    /// <summary>
    /// Save the Master settings to a file on the disk
    /// </summary>
    /// <param name="settings">The MasterSettings object</param>
    public void Save_Settings(MasterSettings settings)
    {
        // Convert object to json
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        string settings_json = JsonSerializer.Serialize(settings, options);

        // Write to file
        File.WriteAllText(File_Path, settings_json);
    }

    /// <summary>
    /// Load the Master settings from a file
    /// </summary>
    /// <param name="path">Alternate path to the file</param>
    /// <returns>Returns the MasterSettings object</returns>
    /// <exception cref="FileNotFoundException">The file does not exist</exception>
    /// <exception cref="FileLoadException">The file contains unknown contents</exception>
    public MasterSettings Load_Master_Settings(string path = null)
    {
        if (path == null)
        {
            path = File_Path;
        }

        string json_string;

        try
        {
            json_string = FileHandler.Read_File_Contents(path);
        }
        catch (FileNotFoundException)
        {
            throw new FileNotFoundException("Settings file missing!");
        }

        try
        {
            // Are we loading MasterSettings?
            return JsonSerializer.Deserialize<MasterSettings>(json_string, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw new FileLoadException("Settings file has unknown contents!");
        }
    }

    /// <summary>
    /// Load the Client settings from a file
    /// </summary>
    /// <param name="path">Alternate path to the file</param>
    /// <returns>Returns the ClientSettings object</returns>
    /// <exception cref="FileNotFoundException">The file does not exist</exception>
    /// <exception cref="FileLoadException">The file contains unknown contents</exception>
    public ClientSettings Load_Client_Settings()
    {
        string json_string;

        try
        {
            json_string = FileHandler.Read_File_Contents(File_Path);
        }
        catch (FileNotFoundException)
        {
            throw new FileNotFoundException("Settings file missing!");
        }

        try
        {
            // Are we loading MasterSettings?
            return JsonSerializer.Deserialize<ClientSettings>(json_string);
        }
        catch
        {
            throw new FileLoadException("Settings file has unknown contents!");
        }
    }
}

/// <summary>
/// This class provides functions to collect debug data about the system
/// </summary>
public class DataCollector
{
    /// <summary>
    /// Get the name of the OS
    /// </summary>
    /// <returns>Returns the name of the OS as a string</returns>
    public static string Get_OS_Description()
    {
        return System.Runtime.InteropServices.RuntimeInformation.OSDescription;
    }

    /// <summary>
    /// Get the version of the OS
    /// </summary>
    /// <returns>Returns the version of the OS as a string</returns>
    public static string Get_OS_Version()
    {
        return Environment.OSVersion.Version.ToString();
    }

    /// <summary>
    /// Get the architecture of the system
    /// </summary>
    /// <returns>Returns the architecture of the system as a string</returns>
    public static string Get_OS_Architecture()
    {
        return System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();
    }

    /// <summary>
    /// Get the count of CPU cores
    /// </summary>
    /// <returns>Returns the count of CPU cores as integer</returns>
    public static int Get_CPU_Count()
    {
        return Environment.ProcessorCount;
    }

    /// <summary>
    /// Get the count of GPUs
    /// </summary>
    /// <returns>Returns the count of GPUs as integer</returns>
    public static int Get_GPU_Count()
    {
        return -1;
    }

    /// <summary>
    /// Get the amount of System RAM
    /// </summary>
    /// <returns>Returns the amount of RAM as integer</returns>
    public static int Get_RAM()
    {
        return -1;
    }
}