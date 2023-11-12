public class ClientResponse
{
    public string Message { get; set; }
    public List<Blender> Blender_Installations { get; set; }
    public int RAM_Use_Limit { get; set; }
    public float File_Size_Limit { get; set; }
    public float Render_Time_Limit { get; set; }
    public List<Frame> Frames { get; set; }
    public bool Is_Windows { get; set; }
}

public class MasterResponse
{
    public string Message { get; set; }
    public string Connection_String { get; set; }
    public int File_transfer_Mode { get; set; } //0=TCP;1=SMB;2=FTP
    public bool Use_SID_Temporal { get; set; } = false;
    public string ID { get; set; }
    public float File_Size { get; set; }
    public string Render_Engine { get; set; }
    public string File_Format { get; set; }
    public List<Frame> Frames { get; set; }
    public int Frame_Step { get; set; } = 1;
}