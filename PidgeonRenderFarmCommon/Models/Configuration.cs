using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PidgeonRenderFarm.Common.Database.Models;
using Formatting = Newtonsoft.Json.Formatting;

namespace PidgeonRenderFarm.Common.Models;

public abstract class Configuration<TConfiguration> : BaseEntity
{
    [JsonProperty(nameof(LogLevel))]
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;

    [JsonProperty(nameof(IPAddress))]
    [XmlElement(IsNullable = true)]
    public string? IPAddressString { get; set; }

    private IPAddress? ipAddress;
    public IPAddress? GetIPAddress()
    {
        if (string.IsNullOrEmpty(IPAddressString))
        {
            SetIPAddress(null);
        }
        else
        {
            SetIPAddress(IPAddress.Parse(IPAddressString));
        }

        return ipAddress;
    }
    public void SetIPAddress(IPAddress? value)
    {
        if (!Equals(ipAddress, value))
        {
            ipAddress = value;
            IPAddressString = value?.ToString();
        }
    }
    
    public ushort Port { get; set; } = 16186;
    
    #region Serialization
    #region XML
    private static readonly XmlSerializer _xmlSerializer = new XmlSerializer(typeof(TConfiguration));
    private static readonly XmlWriterSettings _xmlWriterSettings = new XmlWriterSettings()
    {
        Encoding = Encoding.UTF8,
        Indent = true,
        OmitXmlDeclaration = true,
        NamespaceHandling = NamespaceHandling.OmitDuplicates,
        CloseOutput = true,
        WriteEndDocumentOnClose = true,
    };
    private static readonly XmlSerializerNamespaces _xmlSerializerNamespaces = new XmlSerializerNamespaces([
        new XmlQualifiedName("", "")
    ]);
    public void SaveToXAML()
    {
        using (FileStream fileStream = new FileStream("config.xml", FileMode.Create, FileAccess.Write))
        {
            using (XmlWriter xmlWriter = XmlWriter.Create(fileStream, _xmlWriterSettings))
            {
                _xmlSerializer.Serialize(xmlWriter, this, _xmlSerializerNamespaces);
            }
        }
    }
    public static TConfiguration LoadFromXAML<TConfiguration>()
    {
        using (FileStream fileStream = new FileStream("config.xml", FileMode.Open, FileAccess.Read))
        {
            using (XmlReader xmlReader = XmlReader.Create(fileStream))
            {
                return (TConfiguration)_xmlSerializer.Deserialize(xmlReader);
            }
        }
    }
    #endregion

    #region Json
    protected static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
    {

    };
    public void SaveToJson()
    {
        using (FileStream fileStream = new FileStream("config.json", FileMode.Create, FileAccess.Write))
        {
            using (StreamWriter streamWriter = new StreamWriter(fileStream))
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented, _jsonSerializerSettings);
                streamWriter.Write(json);
            }
        }
    }
    public static TConfiguration LoadFromJson<TConfiguration>()
    {
        using (FileStream fileStream = new FileStream("config.json", FileMode.Open, FileAccess.Read))
        {
            using (StreamReader streamReader = new StreamReader(fileStream))
            {
                string json = streamReader.ReadToEnd();
                return JsonConvert.DeserializeObject<TConfiguration>(json);
            }
        }
    }
    #endregion
    #endregion
}