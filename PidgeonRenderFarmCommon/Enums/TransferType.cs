namespace PidgeonRenderFarm.Common.Enums;

public enum TransferType
{
    TCP, // can be unstable
    Restful, // does this even work?
    SMB, // legacy to be avoided
    FTP, // legacy to be avoided
    Copy,
    SecureCopy
}