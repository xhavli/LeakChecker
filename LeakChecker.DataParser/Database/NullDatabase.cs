using LeakChecker.DataParser.Content;
using LeakChecker.DataParser.Stats.Parse;

namespace LeakChecker.DataParser.Database;

public class NullDatabase : IDatabase
{
    public Task SaveUserOne(Dictionary<ItemEnum, List<string>> record, Guid parseId) => Task.CompletedTask;
    public Task SaveUserMany(List<Dictionary<ItemEnum, List<string>>> records, Guid parseId) => Task.CompletedTask;
    public Task SaveParseOne(ParseStats stats) => Task.CompletedTask;
    public Task SaveParseMany(List<ParseStats> stats) => Task.CompletedTask;
}
