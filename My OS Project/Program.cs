class Program
{
    static void Main()
    {
        // Initialize virtual disk
        bool firstRun = VirtualDisk.Initialize("VirtualDisk.bin");

        // Core components
        Superblock superblock = new Superblock();
        FAT fat = new FAT();
        DirectoryManager directoryManager = new DirectoryManager(fat);
        FileSystem fileSystem = new FileSystem(fat, directoryManager);

        // First run initialization
        if (firstRun)
        {
            superblock.Initialize();
            fat.Initialize();
            fat.Save();
        }
        else
        {
            fat.Load();
        }

        // Start shell
        Shell shell = new Shell(fileSystem, directoryManager, fat);
        shell.Run();
    }
}
