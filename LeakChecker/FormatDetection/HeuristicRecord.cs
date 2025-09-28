using LeakChecker.ContentDetection;

namespace LeakChecker.FormatDetection;

public class HeuristicRecord
{
    public ItemEnum Attribute { get; set; }
    public int TokenStart { get; set; }
    public int DelimiterCountInside { get; set; }
}