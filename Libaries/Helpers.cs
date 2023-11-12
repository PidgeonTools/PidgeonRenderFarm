using System.Net.Sockets;
using System.Net;

public class Helpers
{
    // Check if string is a valid port
    public static bool Is_Port(string port)
    {
        int p;
        // Check if string is number
        if (!int.TryParse(port, out p))
        {
            return false;
        }

        // Check if number is between 1 and 65535
        return (p >= 1 && p <= 65535);
    }

    // Convert string to bool, accepts default
    public static bool Parse_Bool(string value, bool def = true)
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

        throw new Exception("Invalid input");

        // if none applies, return error
        //return null;
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

        throw new Exception("Invalid input");

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

        return splitValues.All(r => byte.TryParse(r, out _));
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
                // Return the first matching IPv4
                return ip;
            }
        }

        // If not connected only use local IP
        return IPAddress.Parse("127.0.0.1");
    }

    public static int Bool_To_Int(bool input)
    {
        return input ? 1 : 0;
    }
}