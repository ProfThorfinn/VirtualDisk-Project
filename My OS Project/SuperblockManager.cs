public class Superblock
{
    public void Initialize()
    {
        VirtualDisk.WriteCluster(
            FSConstants.SUPERBLOCK_CLUSTER,
            new byte[FSConstants.CLUSTER_SIZE]);
    }
}
