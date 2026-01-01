using System;

public class FileSystem
{
    private FAT fat;
    private DirectoryManager dir;

    public FileSystem(FAT fat, DirectoryManager dir)
    {
        this.fat = fat;
        this.dir = dir;
    }

    // ================= FILE OPERATIONS =================

    public void CreateFile(int parent, string name)
    {
        if (dir.FindEntry(parent, name) != null)
            throw new Exception("File already exists");

        dir.AddEntry(parent, new DirectoryEntry
        {
            Name = name,
            Attribute = 0x20, // file
            FirstCluster = 0,
            FileSize = 0
        });
    }

    public void WriteFile(int parent, string name, byte[] data)
    {
        DirectoryEntry f = dir.FindEntry(parent, name);
        if (f == null || f.Attribute != 0x20)
            throw new Exception("File not found");

        if (f.FirstCluster != 0)
            fat.FreeChain(f.FirstCluster);

        int need = (int)Math.Ceiling(data.Length / (double)FSConstants.CLUSTER_SIZE);
        int start = need > 0 ? fat.AllocateChain(need) : 0;

        int offset = 0;
        int cur = start;

        while (cur != -1 && offset < data.Length)
        {
            byte[] buf = new byte[FSConstants.CLUSTER_SIZE];
            int len = Math.Min(FSConstants.CLUSTER_SIZE, data.Length - offset);
            Array.Copy(data, offset, buf, 0, len);

            VirtualDisk.WriteCluster(cur, buf);

            offset += FSConstants.CLUSTER_SIZE;
            cur = fat.GetFatEntry(cur);
        }

        f.FirstCluster = start;
        f.FileSize = data.Length;

        dir.RemoveEntry(parent, name);
        dir.AddEntry(parent, f);
    }

    public byte[] ReadFile(int parent, string name)
    {
        DirectoryEntry f = dir.FindEntry(parent, name);
        if (f == null || f.Attribute != 0x20)
            throw new Exception("File not found");

        byte[] result = new byte[f.FileSize];
        int offset = 0;
        int cur = f.FirstCluster;

        while (cur != -1 && offset < f.FileSize)
        {
            byte[] data = VirtualDisk.ReadCluster(cur);
            int len = Math.Min(FSConstants.CLUSTER_SIZE, f.FileSize - offset);
            Array.Copy(data, 0, result, offset, len);

            offset += FSConstants.CLUSTER_SIZE;
            cur = fat.GetFatEntry(cur);
        }

        return result;
    }

    public void DeleteFile(int parent, string name)
    {
        DirectoryEntry f = dir.FindEntry(parent, name);
        if (f == null || f.Attribute != 0x20)
            throw new Exception("File not found");

        if (f.FirstCluster != 0)
            fat.FreeChain(f.FirstCluster);

        dir.RemoveEntry(parent, name);
    }

    // ================= DIRECTORY OPERATIONS =================

    public void CreateDirectory(int parent, string name)
    {
        if (dir.FindEntry(parent, name) != null)
            throw new Exception("Directory already exists");

        int cluster = fat.AllocateChain(1);

        VirtualDisk.WriteCluster(
            cluster,
            new byte[FSConstants.CLUSTER_SIZE]);

        dir.AddEntry(parent, new DirectoryEntry
        {
            Name = name,
            Attribute = 0x10, // directory
            FirstCluster = cluster,
            FileSize = 0
        });
    }

    public void RemoveDirectory(int parent, string name)
    {
        DirectoryEntry d = dir.FindEntry(parent, name);

        if (d == null)
            throw new Exception("Directory not found");

        if (d.Attribute != 0x10)
            throw new Exception("Not a directory");

        if (dir.ReadDirectory(d.FirstCluster).Count > 0)
            throw new Exception("Directory not empty");

        fat.FreeChain(d.FirstCluster);
        dir.RemoveEntry(parent, name);
    }
}
