namespace LeakChecker.DataParser.Utilities;

public static class SizeEnum
{
    public const long Byte = 1L;
    public const long KiloByte = Byte * 1024L;
    public const long MegaByte = KiloByte * 1024L;
    public const long GigaByte = MegaByte * 1024L;
    public const long TeraByte = GigaByte * 1024L;
}