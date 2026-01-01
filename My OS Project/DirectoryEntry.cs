public class DirectoryEntry
{
    public string Name;
    public byte Attribute; // 0x10 dir , 0x20 file
    public int FirstCluster;
    public int FileSize;
}
