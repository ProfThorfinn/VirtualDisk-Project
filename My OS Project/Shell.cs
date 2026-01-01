using System;
using System.Text;
using System.Collections.Generic;

public class Shell
{
    private FileSystem fs;
    private DirectoryManager dir;
    private FAT fat;

    private int currentDir;
    private Stack<int> dirStack = new Stack<int>(); // لدعم cd ..

    public Shell(FileSystem fs, DirectoryManager dir, FAT fat)
    {
        this.fs = fs;
        this.dir = dir;
        this.fat = fat;
        currentDir = FSConstants.ROOT_DIR_FIRST_CLUSTER;
    }

    // ================= RUN =================
    public void Run()
    {
        while (true)
        {
            Console.Write("H:\\> ");
            string input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            try
            {
                Execute(input.Trim());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }

    // ================= EXECUTE =================
    private void Execute(string input)
    {
        List<string> args = Parse(input);
        if (args.Count == 0) return;

        string cmd = args[0].ToLower();

        switch (cmd)
        {
            case "help": Help(); break;
            case "exit": Exit(); break;
            case "clear": Clear(); break;

            case "ls": Ls(); break;
            case "cd": Cd(args); break;

            case "mkdir": Mkdir(args); break;
            case "rmdir": Rmdir(args); break;

            case "touch": Touch(args); break;
            case "rm": Rm(args); break;

            case "cat": Cat(args); break;
            case "echo": Echo(args); break;

            case "cp": Cp(args); break;
            case "mv": Mv(args); break;

            default:
                Console.WriteLine("Unknown command");
                break;
        }
    }

    // ================= COMMANDS =================

    private void Help()
    {
        Console.WriteLine("ls              List directory contents");
        Console.WriteLine("cd <dir>        Change current directory");
        Console.WriteLine("cd ..           Go to parent directory");
        Console.WriteLine("mkdir <dir>     Create a new directory");
        Console.WriteLine("rmdir <dir>     Remove an empty directory");
        Console.WriteLine("touch <file>    Create an empty file");
        Console.WriteLine("rm <file>       Delete a file");
        Console.WriteLine("cat <file>      Display file contents");
        Console.WriteLine("echo \"text\" <file>          Write text to file (overwrite)");
        Console.WriteLine("echo \"text\" <file> --append Append text to file");
        Console.WriteLine("cp <src> <dst>  Copy file");
        Console.WriteLine("mv <src> <dst>  Move or rename file");
        Console.WriteLine("clear           Clear the screen");
        Console.WriteLine("help            Show available commands");
        Console.WriteLine("exit            Exit the shell");
    }


    private void Exit()
    {
        fat.Save();
        VirtualDisk.CloseDisk();
        Environment.Exit(0);
    }

    private void Clear()
    {
        Console.Clear();
    }

    private void Ls()
    {
        foreach (var e in dir.ReadDirectory(currentDir))
            Console.WriteLine(e.Name);
    }

    private void Cd(List<string> args)
    {
        if (args.Count < 2)
            throw new Exception("Usage: cd <dir>");

        if (args[1] == "..")
        {
            if (dirStack.Count > 0)
                currentDir = dirStack.Pop();
            return;
        }

        DirectoryEntry d = dir.FindEntry(currentDir, args[1]);
        if (d == null || d.Attribute != 0x10)
            throw new Exception("Directory not found");

        dirStack.Push(currentDir);
        currentDir = d.FirstCluster;
    }

    private void Mkdir(List<string> args)
    {
        if (args.Count < 2)
            throw new Exception("Usage: mkdir <dir>");

        fs.CreateDirectory(currentDir, args[1]);
    }

    private void Rmdir(List<string> args)
    {
        if (args.Count < 2)
            throw new Exception("Usage: rmdir <dir>");

        fs.RemoveDirectory(currentDir, args[1]);
    }

    private void Touch(List<string> args)
    {
        if (args.Count < 2)
            throw new Exception("Usage: touch <file>");

        if (dir.FindEntry(currentDir, args[1]) == null)
            fs.CreateFile(currentDir, args[1]);
    }

    private void Rm(List<string> args)
    {
        if (args.Count < 2)
            throw new Exception("Usage: rm <file>");

        fs.DeleteFile(currentDir, args[1]);
    }

    private void Cat(List<string> args)
    {
        if (args.Count < 2)
            throw new Exception("Usage: cat <file>");

        byte[] data = fs.ReadFile(currentDir, args[1]);
        Console.WriteLine(Encoding.ASCII.GetString(data));
    }

    private void Echo(List<string> args)
    {
        if (args.Count < 3)
            throw new Exception("Usage: echo \"text\" <file> [--append]");

        bool append = args.Contains("--append");
        string text = args[1];
        string file = args[2];

        byte[] oldData = new byte[0];
        if (append && dir.FindEntry(currentDir, file) != null)
            oldData = fs.ReadFile(currentDir, file);

        byte[] newData = Encoding.ASCII.GetBytes(text);
        byte[] finalData = append ? Combine(oldData, newData) : newData;

        if (dir.FindEntry(currentDir, file) == null)
            fs.CreateFile(currentDir, file);

        fs.WriteFile(currentDir, file, finalData);
    }

    private void Cp(List<string> args)
    {
        if (args.Count < 3)
            throw new Exception("Usage: cp <src> <dst>");

        byte[] data = fs.ReadFile(currentDir, args[1]);
        fs.CreateFile(currentDir, args[2]);
        fs.WriteFile(currentDir, args[2], data);
    }

    private void Mv(List<string> args)
    {
        if (args.Count < 3)
            throw new Exception("Usage: mv <src> <dst>");

        byte[] data = fs.ReadFile(currentDir, args[1]);
        fs.CreateFile(currentDir, args[2]);
        fs.WriteFile(currentDir, args[2], data);
        fs.DeleteFile(currentDir, args[1]);
    }

    // ================= HELPERS =================

    private List<string> Parse(string input)
    {
        List<string> tokens = new List<string>();
        bool quote = false;
        string current = "";

        foreach (char c in input)
        {
            if (c == '"')
            {
                quote = !quote;
                continue;
            }

            if (c == ' ' && !quote)
            {
                if (current != "")
                {
                    tokens.Add(current);
                    current = "";
                }
            }
            else
            {
                current += c;
            }
        }

        if (current != "")
            tokens.Add(current);

        return tokens;
    }

    private byte[] Combine(byte[] a, byte[] b)
    {
        byte[] r = new byte[a.Length + b.Length];
        Array.Copy(a, 0, r, 0, a.Length);
        Array.Copy(b, 0, r, a.Length, b.Length);
        return r;
    }
}
