using System.Numerics;

namespace Libraries.Models;

public class ProjectInfo
{
    public string Render_Engine { get; set; }
    public float Render_Time { get; set; } = 0.0f;
    public int RAM_Use { get; set; } = 0;
    public string File_Format { get; set; }
    public int First_Frame { get; set; }
    public int Last_Frame { get; set; }
    public int Frame_Step { get; set; }
}

// Project object class
public class Project
{
    public string ID { get; set; }
    public string Blender_Version { get; set; }
    public string Full_Path_Blend { get; set; }
    public int File_transfer_Mode { get; set; } //0=TCP;1=SMB;2=FTP
    public bool Use_SID_Temporal { get; set; } = false;
    public bool Use_SFR { get; set; } = false;
    public bool Render_Test_Frame { get; set; }
    public string Render_Engine { get; set; }
    public string Output_File_Format { get; set; }
    public bool Video_Generate { get; set; }
    public int Video_FPS { get; set; }
    public string Video_Rate_Control { get; set; } // CBR, CRF
    public int Video_Rate_Control_Value { get; set; }
    public bool Video_Resize { get; set; }
    public Vector2 Video_Dimensions { get; set; }
    public int Batch_Size { get; set; } = 1;
    public float Time_Per_Frame { get; set; } = 0;
    public int RAM_Use { get; set; } = 0;
    public int First_Frame { get; set; }
    public int Last_Frame { get; set; }
    public int Frame_Step { get; set; } = 1;
    public int Frames_Total { get; set; }
    public bool Keep_ZIP { get; set; }
    public bool Download_Remote_Input { get; set; }
}

public static class FileTransferMode
{
    public static readonly int TCP = 0;
    public static readonly int SMB = 1;
    public static readonly int FTP = 2;
}