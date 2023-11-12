using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Data;

public static class DBHandler
{
    private static DBConnection DB;
    private static string Project_ID;

    private static SqliteConnection Log_SQLite_Connection;
    private static SqliteConnection Project_SQLite_Connection;

    public static void Initialize(DBConnection conn)
    {
        DB = conn;

        Initialize_Log_Table();
    }

    private static void Initialize_Log_Table()
    {
        if (DB.Mode == DBMode.SQLite)
        {
            Log_SQLite_Connection = new SqliteConnection($"Data Source={Path.Join(DB.Path, "Log.db")}");
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
                query = Log_SQLite_Connection.CreateCommand();
                query.CommandText =
                "CREATE TABLE \"LOG\" " +
                "(\"ID\" INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, " +
                "\"TIME\" TEXT NOT NULL, " +
                "\"LEVEL\" TEXT NOT NULL, " +
                "\"MODULE\" TEXT NOT NULL, " +
                "\"MESSAGE\" TEXT NOT NULL)";
                query.ExecuteNonQuery();
            }
        }
    }

    public static void Initialize_Project_Table(string project_id, bool load = false)
    {
        Project_ID = project_id;

        if (DB.Mode == DBMode.SQLite)
        {
            Project_SQLite_Connection = new SqliteConnection($"Data Source={Path.Join(DB.Path, "Project.db")}");
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
                    query = Project_SQLite_Connection.CreateCommand();
                    query.CommandText =
                    $"CREATE TABLE \"{Project_ID}\" " +
                    "(\"ID\" INTEGER PRIMARY KEY NOT NULL, " +
                    "\"STATE\" TEXT NOT NULL," +
                    "\"Client\" TEXT)";
                    query.ExecuteNonQuery();
                }
                else
                {
                    throw new DataException("Project not found in DB");
                }
            }
        }
    }

    #region Table: PROJECT_FRAMES
    public static int Insert_Frames_Table(int first_frame, int last_frame, int frame_step = 1)
    {
        int frames_total = 0;

        for (int i = first_frame; i <= last_frame; i += frame_step)
        {
            if (DB.Mode == DBMode.SQLite)
            {
                SqliteCommand query = Project_SQLite_Connection.CreateCommand();
                query.CommandText =
                $"INSERT INTO \"{Project_ID}\" " +
                "(ID, STATE) " +
                "VALUES " +
                $"({i}, \"Open\");";

                Console.WriteLine(query.CommandText);
                query.ExecuteNonQuery();
            }

            frames_total++;
        }

        return frames_total;
    }

    public static List<int> Select_Frames_Table_All_Open()
    {
        List<int> frames = new List<int>();

        if (DB.Mode == DBMode.SQLite)
        {
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

    public static void Update_Frames_Table(string state, int first_frame, int last_frame, int frame_step = 1)
    {
        for (int i = first_frame; i <= last_frame; i += frame_step)
        {
            if (DB.Mode == DBMode.SQLite)
            {
                SqliteCommand query = Project_SQLite_Connection.CreateCommand();
                query.CommandText =
                $"UPDATE \"{Project_ID}\" " +
                $"SET STATE = \"{state}\" " +
                $"WHERE ID = {i}";
                query.ExecuteNonQuery();
            }
        }
    }
    public static void Update_Frames_Table(List<Frame> frames)
    {

        foreach (Frame frame in frames)
        {
            if (DB.Mode == DBMode.SQLite)
            {
                SqliteCommand query = Project_SQLite_Connection.CreateCommand();
                query.CommandText =
                $"UPDATE \"{Project_ID}\" " +
                $"SET STATE = \"{frame.State}\" " +
                (string.IsNullOrEmpty(frame.IPv4) ? "" : $", Client = \"{frame.IPv4}\" ") +
                $"WHERE ID = {frame.Id}";
                query.ExecuteNonQuery();
            }
        }
    }
    #endregion

    #region Table: LOG
    public static void Insert_Log_Table(string time, string level, string module, string message)
    {
        if (DB.Mode == DBMode.SQLite)
        {
            SqliteCommand query = Log_SQLite_Connection.CreateCommand();
            query.CommandText =
            "INSERT INTO LOG " +
            "(TIME, LEVEL, MODULE, MESSAGE) " +
            "VALUES " +
            $"(\"{time}\", \"{level}\", \"{module}\", \"{message}\");";
            query.ExecuteNonQuery();
        }
    }
    #endregion

    public static void Shutdown_Database()
    {
        if (DB.Mode == DBMode.SQLite)
        {
            Project_SQLite_Connection.Close();
            Log_SQLite_Connection.Close();
        }
    }
}

public class FileHandler
{
    public static void Save_SystemInfo(string file_path, SystemInfo info)
    {
        // Convert object to json
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        string settings_json = JsonSerializer.Serialize(info, options);

        // Write to file
        File.WriteAllText(file_path, settings_json);
    }
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

public static class ProjectFileHandler
{
    public static string Bin_Directory;

    public static void Save_Project(Project project, string file_path)
    {
        // Convert object to json
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        string settings_json = JsonSerializer.Serialize(project, options);

        // Write to file
        File.WriteAllText(file_path, settings_json);
    }

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

public class SettingsFileHandler
{
    private string File_Path;

    public SettingsFileHandler(string file_path)
    {
        File_Path = file_path;
    }

    public void Save_Settings(ClientSettings settings)
    {
        // Convert object to json
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        string settings_json = JsonSerializer.Serialize(settings, options);

        // Write to file
        File.WriteAllText(File_Path, settings_json);
    }
    public void Save_Settings(MasterSettings settings)
    {
        // Convert object to json
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
        string settings_json = JsonSerializer.Serialize(settings, options);

        // Write to file
        File.WriteAllText(File_Path, settings_json);
    }

    public MasterSettings Load_Master_Settings()
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
            return JsonSerializer.Deserialize<MasterSettings>(json_string, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw new FileLoadException("Settings file has unknown contents!");
        }
    }
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

public class DataCollector
{
    public static string Get_OS_Description()
    {
        return System.Runtime.InteropServices.RuntimeInformation.OSDescription;
    }
    public static string Get_OS_Version()
    {
        return Environment.OSVersion.Version.ToString();
    }
    public static string Get_OS_Architecture()
    {
        return System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();
    }

    public static int Get_CPU_Count()
    {
        return Environment.ProcessorCount;
    }
    public static int Get_GPU_Count()
    {
        return -1;
    }

    public static int Get_RAM()
    {
        return -1;
    }
}