using System;
using System.IO;

public static class VirtualDisk
{
    private static FileStream disk;

    public static bool Initialize(string path)
    {
        bool first = !File.Exists(path);
        disk = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);

        if (first)
            disk.SetLength(FSConstants.CLUSTER_SIZE * FSConstants.CLUSTER_COUNT);

        return first;
    }

    private static void Validate(int cluster)
    {
        if (cluster < 0 || cluster >= FSConstants.CLUSTER_COUNT)
            throw new Exception("Invalid cluster number");
    }

    public static byte[] ReadCluster(int cluster)
    {
        Validate(cluster);
        byte[] b = new byte[FSConstants.CLUSTER_SIZE];
        disk.Seek(cluster * FSConstants.CLUSTER_SIZE, SeekOrigin.Begin);
        disk.Read(b, 0, b.Length);
        return b;
    }

    public static void WriteCluster(int cluster, byte[] data)
    {
        Validate(cluster);
        if (data.Length != FSConstants.CLUSTER_SIZE)
            throw new Exception("Invalid cluster size");

        disk.Seek(cluster * FSConstants.CLUSTER_SIZE, SeekOrigin.Begin);
        disk.Write(data, 0, data.Length);
        disk.Flush();
    }

    public static void CloseDisk()
    {
        disk?.Close();
    }
}
