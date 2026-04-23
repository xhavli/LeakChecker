using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Stats.Parse;

namespace LeakChecker.DataParser.Database;

public interface IDatabase
{
    Task SaveUserOne(Dictionary<ItemEnum, List<string>> record, Guid parseId);
    Task SaveUserMany(List<Dictionary<ItemEnum, List<string>>> records, Guid parseId);
    Task SaveParseOne(ParseStats stats);
    Task SaveParseMany(List<ParseStats> stats);
}