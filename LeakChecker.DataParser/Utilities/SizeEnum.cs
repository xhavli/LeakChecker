namespace LeakChecker.DataParser.Utilities;

public static class SizeEnum
{
    private const int Byte = 1;
    public const int KiloByte = Byte * 1024;
    public const int MegaByte = KiloByte * 1024;
    public const int GigaByte = MegaByte * 1024;
    public const long TeraByte = GigaByte * 1024L;
}