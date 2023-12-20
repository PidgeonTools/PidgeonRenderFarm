using System.Net.Sockets;
using System.Net;

namespace Libraries;

/// <summary>
/// This class provides various functions
/// </summary>
public class Helpers
{
    /// <summary>
    /// Function to check if a given string is a port
    /// </summary>
    /// <param name="port">The input string to be checked</param>
    /// <returns>Returns true if the string is a port, else false</returns>
    public static bool Is_Port(string port)
    {
        int p;
        // Check if string is number
        if (!int.TryParse(port, out p))
        {
            return false;
        }

        // Check if number is between 1 and 65535
        return p >= 1 && p <= 65535;
    }

    // Convert string to bool, accepts default
    /// <summary>
    /// Convert a string to a boolean, has the option for default values
    /// </summary>
    /// <param name="value">The string the user input</param>
    /// <param name="def">The default value</param>
    /// <returns>Returns the computer version of yes or no</returns>
    /// <exception cref="Exception">If the provided input does not match anything this exception is raised</exception>
    public static bool Parse_Bool(string value, bool def = true)
    {
        // If human is not sure, return default value/let developer decide
        if (string.IsNullOrWhiteSpace(value))
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

    /// <summary>
    /// Check if a string is a IPv4
    /// Credit: https://stackoverflow.com/users/961113/habib
    /// </summary>
    /// <param name="ipString"></param>
    /// <returns>Returns true if the string is a IPv4, else false</returns>
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
    /// <summary>
    /// Check if a combination of a IPv4 and a port (separated by ":") is a valid combination
    /// </summary>
    /// <param name="value">The combination of the IPv4 and port</param>
    /// <returns>Returns true if the IPv4 and port are valid, else false</returns>
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

    /// <summary>
    /// Get the local IPv4 of a device, default to localhost
    /// </summary>
    /// <returns>Returns the IP as IPAddress object</returns>
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

    /// <summary>
    /// Convert a boolean to an integer
    /// </summary>
    /// <param name="input"></param>
    /// <returns>Returns 1 if the bool is true, else 0</returns>
    public static int Bool_To_Int(bool input)
    {
        return input ? 1 : 0;
    }
}