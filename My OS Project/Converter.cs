using System.Text;

public static class Converter
{
    public static byte[] StringToBytes(string s, int len)
    {
        byte[] b = new byte[len];
        byte[] src = Encoding.ASCII.GetBytes(s);
        System.Array.Copy(src, b, System.Math.Min(len, src.Length));
        return b;
    }

    public static string BytesToString(byte[] b)
    {
        return Encoding.ASCII.GetString(b).Trim('\0', ' ');
    }
}
