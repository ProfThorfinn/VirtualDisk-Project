using System;
using System.Collections.Generic;

public class DirectoryManager
{
    private FAT fat;

    public DirectoryManager(FAT fat)
    {
        this.fat = fat;
    }

    // ================= NAME FORMAT (8.3) =================
    public string Format83(string name)
    {
        name = name.ToUpper();

        if (name.Contains("."))
        {
            string[] p = name.Split('.');
            string n = p[0].PadRight(8).Substring(0, 8);
            string e = p[1].PadRight(3).Substring(0, 3);
            return n + e;
        }

        return name.PadRight(11).Substring(0, 11);
    }

    // ================= READ DIRECTORY =================
    public List<DirectoryEntry> ReadDirectory(int start)
    {
        List<DirectoryEntry> list = new List<DirectoryEntry>();
        int cur = start;

        while (cur != -1 && cur != 0)
        {
            byte[] data = VirtualDisk.ReadCluster(cur);

            for (int i = 0; i < FSConstants.CLUSTER_SIZE; i += 32)
            {
                if (data[i] == 0x00)
                    continue;

                byte[] nameBytes = new byte[11];
                Array.Copy(data, i, nameBytes, 0, 11);

                DirectoryEntry entry = new DirectoryEntry
                {
                    Name = Converter.BytesToString(nameBytes),
                    Attribute = data[i + 11],
                    FirstCluster = BitConverter.ToInt32(data, i + 12),
                    FileSize = BitConverter.ToInt32(data, i + 16)
                };

                list.Add(entry);
            }

            cur = fat.GetFatEntry(cur);
        }

        return list;
    }

    // ================= FIND ENTRY (FIXED) =================
    public DirectoryEntry FindEntry(int dir, string name)
    {
        string target = Format83(name);

        foreach (DirectoryEntry e in ReadDirectory(dir))
        {
            if (e.Name.Trim()
                .Equals(target.Trim(), StringComparison.OrdinalIgnoreCase))
                return e;
        }

        return null;
    }

    // ================= ADD ENTRY =================
    public void AddEntry(int dir, DirectoryEntry entry)
    {
        entry.Name = Format83(entry.Name);

        if (FindEntry(dir, entry.Name) != null)
            throw new Exception("Entry already exists");

        int cur = dir;

        while (true)
        {
            byte[] data = VirtualDisk.ReadCluster(cur);

            for (int i = 0; i < FSConstants.CLUSTER_SIZE; i += 32)
            {
                if (data[i] == 0x00)
                {
                    WriteEntry(data, i, entry);
                    VirtualDisk.WriteCluster(cur, data);
                    return;
                }
            }

            int next = fat.GetFatEntry(cur);
            if (next == -1)
                break;

            cur = next;
        }

        // directory full → allocate new cluster
        int newCluster = fat.AllocateChain(1);
        fat.SetFatEntry(cur, newCluster);
        fat.SetFatEntry(newCluster, -1);

        byte[] newData = new byte[FSConstants.CLUSTER_SIZE];
        WriteEntry(newData, 0, entry);
        VirtualDisk.WriteCluster(newCluster, newData);
    }

    // ================= REMOVE ENTRY (FREES CLUSTERS) =================
    public void RemoveEntry(int dir, string name)
    {
        string target = Format83(name);
        int cur = dir;

        while (cur != -1 && cur != 0)
        {
            byte[] data = VirtualDisk.ReadCluster(cur);

            for (int i = 0; i < FSConstants.CLUSTER_SIZE; i += 32)
            {
                byte[] nameBytes = new byte[11];
                Array.Copy(data, i, nameBytes, 0, 11);

                string current =
                    Converter.BytesToString(nameBytes).Trim();

                if (current.Equals(target.Trim(),
                                   StringComparison.OrdinalIgnoreCase))
                {
                    int firstCluster =
                        BitConverter.ToInt32(data, i + 12);

                    if (firstCluster != 0)
                        fat.FreeChain(firstCluster);

                    data[i] = 0x00; // mark entry as empty
                    VirtualDisk.WriteCluster(cur, data);
                    return;
                }
            }

            cur = fat.GetFatEntry(cur);
        }

        throw new Exception("Entry not found");
    }

    // ================= WRITE ENTRY =================
    private void WriteEntry(byte[] data, int offset, DirectoryEntry e)
    {
        byte[] nameBytes =
            Converter.StringToBytes(e.Name, 11);

        Array.Copy(nameBytes, 0, data, offset, 11);
        data[offset + 11] = e.Attribute;
        Array.Copy(BitConverter.GetBytes(e.FirstCluster), 0, data, offset + 12, 4);
        Array.Copy(BitConverter.GetBytes(e.FileSize), 0, data, offset + 16, 4);
    }
}
