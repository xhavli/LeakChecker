using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Database;
using LeakChecker.UI.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MongoDB.Bson;

namespace LeakChecker.UI.Components;

public class SearchIdentityBase : ComponentBase
{
    protected static readonly ItemEnum[] SearchableItems =
        Enum.GetValues<ItemEnum>()
            .Where(i => i is >= ItemEnum.Mac and <= ItemEnum.Other)
            .Where(i => i is not ItemEnum.NetTicks
                and not ItemEnum.FileTime
                and not ItemEnum.UnixSeconds
                and not ItemEnum.UnixMilliseconds)
            .ToArray();

    private static readonly HashSet<string> ResultFields =
        SearchableItems.Select(i => i.ToString()).ToHashSet();

    protected string SearchValue { get; set; } = string.Empty;
    protected ConditionType SelectedCondition { get; set; } = ConditionType.ExactMatch;
    protected ItemEnum SelectedItem { get; set; } = ItemEnum.Email;

    protected bool IsSearching { get; set; }
    protected bool HasSearched { get; set; }
    protected bool IsLoadingMore { get; set; }
    protected bool HasMore { get; set; }
    protected string? ErrorMessage { get; set; }

    private const int PageSize = 50;
    private ObjectId? _lastId;

    protected List<Dictionary<string, string?>> Results { get; set; } = [];
    protected List<string> ResultColumns { get; set; } = [];
    protected int TotalLoaded => Results.Count;
    protected long? TotalMatched { get; set; }
    protected TimeSpan Elapsed { get; set; }
    private DateTime _searchStart;
    private System.Threading.Timer? _timer;

    private (string field, string value, ConditionType condition) ResolveQuery()
    {
        var raw = SearchValue.Trim();

        if (SelectedItem == ItemEnum.Domain && SelectedCondition == ConditionType.EndsWith)
        {
            var reversed = Reverse(raw.ToLowerInvariant());
            return (nameof(ItemEnum.DomainReversedLowercase), reversed, ConditionType.StartsWith);
        }

        return SelectedItem switch
        {
            ItemEnum.Email    => (nameof(ItemEnum.EmailLowercase),    raw.ToLowerInvariant(), SelectedCondition),
            ItemEnum.Username => (nameof(ItemEnum.UsernameLowercase),  raw.ToLowerInvariant(), SelectedCondition),
            ItemEnum.Name     => (nameof(ItemEnum.NameLowercase),      raw.ToLowerInvariant(), SelectedCondition),
            _                 => (SelectedItem.ToString(),             raw,                    SelectedCondition),
        };
    }

    private static string Reverse(string s)
    {
        var chars = s.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    protected async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(SearchValue) && !IsSearching)
            await RunSearch();
    }

    // Fresh search — resets everything
    protected async Task RunSearch()
    {
        ErrorMessage  = null;
        IsSearching   = true;
        HasSearched   = false;
        HasMore       = false;
        _lastId       = null;
        Results       = [];
        ResultColumns = [];
        TotalMatched = null;
        _searchStart = DateTime.UtcNow;
        _timer?.Dispose();
        _timer = new System.Threading.Timer(_ =>
        {
            Elapsed = DateTime.UtcNow - _searchStart;
            InvokeAsync(StateHasChanged);
        }, null, 0, 1000);

        try
        {
            var (field, value, condition) = ResolveQuery();
            var docs = await MongoDbRepository.SearchUsers(field, condition, value, afterId: null, limit: PageSize);
            

            var docsTask  = MongoDbRepository.SearchUsers(field, condition, value, afterId: null, limit: PageSize);
            var countTask = MongoDbRepository.CountUsers(field, condition, value);

            await Task.WhenAll(docsTask, countTask);

            TotalMatched = countTask.Result;
            ApplyPage(docsTask.Result);

            ApplyPage(docs);
            HasSearched = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Search failed: {ex.Message}";
        }
        finally
        {
            _timer?.Dispose();
            _timer = null;
            Elapsed = DateTime.UtcNow - _searchStart; // final accurate value
            IsSearching = false;
        }
    }

    // Load next page and append
    protected async Task LoadMore()
    {
        if (!HasMore || IsLoadingMore) return;

        IsLoadingMore = true;
        ErrorMessage  = null;

        try
        {
            var (field, value, condition) = ResolveQuery();
            var docs = await MongoDbRepository.SearchUsers(field, condition, value, afterId: _lastId, limit: PageSize);

            ApplyPage(docs);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Load more failed: {ex.Message}";
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    private void ApplyPage(List<BsonDocument> docs)
    {
        if (docs.Count == 0)
        {
            HasMore = false;
            return;
        }

        // Update cursor to last doc's _id
        _lastId = docs[^1]["_id"].AsObjectId;
        HasMore = docs.Count == PageSize;

        // Merge new columns
        var newCols = SearchableItems
            .Select(i => i.ToString())
            .Where(name => docs.Any(d => d.Contains(name)) && !ResultColumns.Contains(name));
        ResultColumns.AddRange(newCols);

        // Map rows
        foreach (var doc in docs)
        {
            var row = new Dictionary<string, string?>();
            foreach (var col in ResultColumns)
            {
                if (!doc.TryGetValue(col, out var bval))
                    continue;

                row[col] = col == nameof(ItemEnum.Hash)
                    ? Formatter.FormatHashes(bval.AsBsonArray)
                    : bval switch
                    {
                        BsonArray arr => string.Join(", ", arr.Select(v => v.ToString())),
                        _             => bval.ToString()
                    };
            }
            Results.Add(row);
        }
    }
}