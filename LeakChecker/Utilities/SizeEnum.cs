namespace LeakChecker.Utilities;

public static class SizeEnum
{
    public const long Byte = 1L;
    public const long Kilobyte = Byte * 1024L;
    public const long Megabyte = Kilobyte * 1024L;
    public const long Gigabyte = Megabyte * 1024L;
    public const long Terabyte = Gigabyte * 1024L;
}