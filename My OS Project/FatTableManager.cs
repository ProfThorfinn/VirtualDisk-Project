using System;
using System.Collections.Generic;

public class FAT
{
    private int[] table = new int[FSConstants.CLUSTER_COUNT];

    public void Initialize()
    {
        for (int i = 0; i < table.Length; i++)
            table[i] = 0;

        table[FSConstants.SUPERBLOCK_CLUSTER] = -1;

        for (int i = FSConstants.FAT_START_CLUSTER;
             i < FSConstants.FAT_START_CLUSTER + FSConstants.FAT_CLUSTER_COUNT; i++)
            table[i] = -1;

        table[FSConstants.ROOT_DIR_FIRST_CLUSTER] = -1;
    }

    public void Load()
    {
        int idx = 0;

        for (int c = FSConstants.FAT_START_CLUSTER;
             c < FSConstants.FAT_START_CLUSTER + FSConstants.FAT_CLUSTER_COUNT; c++)
        {
            byte[] data = VirtualDisk.ReadCluster(c);

            for (int i = 0; i < FSConstants.CLUSTER_SIZE; i += 4)
                table[idx++] = BitConverter.ToInt32(data, i);
        }
    }

    public void Save()
    {
        int idx = 0;

        for (int c = FSConstants.FAT_START_CLUSTER;
             c < FSConstants.FAT_START_CLUSTER + FSConstants.FAT_CLUSTER_COUNT; c++)
        {
            byte[] data = new byte[FSConstants.CLUSTER_SIZE];

            for (int i = 0; i < FSConstants.CLUSTER_SIZE; i += 4)
                Array.Copy(BitConverter.GetBytes(table[idx++]), 0, data, i, 4);

            VirtualDisk.WriteCluster(c, data);
        }
    }

    public int GetFatEntry(int index) => table[index];
    public void SetFatEntry(int index, int value) => table[index] = value;

    public List<int> FollowChain(int start)
    {
        List<int> chain = new List<int>();
        int cur = start;

        while (cur != -1 && cur != 0)
        {
            if (chain.Contains(cur))
                throw new Exception("FAT loop detected");

            chain.Add(cur);
            cur = table[cur];
        }
        return chain;
    }

    public int AllocateChain(int count)
    {
        int first = -1, prev = -1;

        for (int i = FSConstants.DATA_START_CLUSTER;
             i < table.Length && count > 0; i++)
        {
            if (table[i] == 0)
            {
                if (first == -1) first = i;
                if (prev != -1) table[prev] = i;

                prev = i;
                table[i] = -1;
                count--;
            }
        }

        if (count > 0)
            throw new Exception("No disk space");

        return first;
    }

    public void FreeChain(int start)
    {
        int cur = start;
        while (cur != -1 && cur != 0)
        {
            int next = table[cur];
            table[cur] = 0;
            cur = next;
        }
    }
}
