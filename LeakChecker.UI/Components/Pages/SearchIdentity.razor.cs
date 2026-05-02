using LeakChecker.Common.Enums;
using LeakChecker.DataParser.Database;
using LeakChecker.UI.Helpers;
using LeakChecker.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MongoDB.Bson;

namespace LeakChecker.UI.Components;

public class SearchIdentityBase : ComponentBase
{
    [Inject] private IJSRuntime Js { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;
    [Inject] private IDashboardService DashboardService { get; set; } = default!;

    protected static readonly ItemEnum[] SearchableItems =
        Enum.GetValues<ItemEnum>()
            .Where(i => i is >= ItemEnum.MacAddress and <= ItemEnum.Other)
            .Where(i => i is not ItemEnum.NetTicks
                and not ItemEnum.FileTime
                and not ItemEnum.UnixSeconds
                and not ItemEnum.UnixMilliseconds)
            .OrderBy(i => i == ItemEnum.Other)
            .ThenBy(i => i.ToString())
            .ToArray();

    protected string SearchValue { get; set; } = string.Empty;
    protected ConditionType SelectedCondition { get; set; } = ConditionType.StartsWith;
    protected ItemEnum SelectedItem { get; set; } = ItemEnum.Email;

    protected DateTime? DatePart { get; set; }
    protected TimeOnly? TimePart { get; set; }
    private DateTime? _tsFrom;
    private DateTime? _tsTo;

    protected bool IsSearchReady => SelectedItem == ItemEnum.Timestamp
        ? DatePart.HasValue
        : !string.IsNullOrWhiteSpace(SearchValue);

    protected bool IsSearching { get; set; }
    protected bool HasSearched { get; set; }
    protected bool IsLoadingMore { get; set; }
    protected bool HasMore { get; set; }
    protected string? ErrorMessage { get; set; }
    
    private const string SourceColumn = "Source";
    private const int PageSize = 50;
    private ObjectId? _lastId;
    
    private readonly Dictionary<ObjectId, string> _sourceNameCache = [];
    private readonly Dictionary<ObjectId, string> _sourcePathCache = [];
    
    protected List<Dictionary<string, string?>> Results { get; set; } = [];
    protected List<string> ResultColumns { get; set; } = [];
    protected int TotalLoaded => Results.Count;
    protected long? TotalMatched { get; set; }
    protected TimeSpan Elapsed { get; set; }
    private DateTime _searchStart;
    private Timer? _timer;
    
    private (string field, string value, ConditionType condition) ResolveQuery()
    {
        var raw = SearchValue.Trim();

        if (SelectedItem == ItemEnum.Domain)
        {
            string reversed = Reverse(raw.ToLowerInvariant());

            return SelectedCondition switch
            {
                ConditionType.StartsWith =>
                    (nameof(ItemEnum.DomainReversedLowercase), reversed, ConditionType.EndsWith),
                ConditionType.Contains =>
                    (nameof(ItemEnum.DomainReversedLowercase), reversed, ConditionType.Contains),
                ConditionType.EndsWith =>
                    (nameof(ItemEnum.DomainReversedLowercase), reversed, ConditionType.StartsWith),
                ConditionType.ExactMatch =>
                    (nameof(ItemEnum.DomainReversedLowercase), reversed, ConditionType.ExactMatch),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        if (SelectedItem == ItemEnum.Email && SelectedCondition == ConditionType.EndsWith && !raw.Contains('@'))
        {
            string reversed = Reverse(raw.ToLowerInvariant());
            return (nameof(ItemEnum.DomainReversedLowercase), reversed, ConditionType.StartsWith);
        }
        
        return SelectedItem switch
        {
            ItemEnum.Name     => (nameof(ItemEnum.NameLowercase),    raw.ToLowerInvariant(), SelectedCondition),
            ItemEnum.Email    => (nameof(ItemEnum.EmailLowercase),   raw.ToLowerInvariant(), SelectedCondition),
            ItemEnum.Username => (nameof(ItemEnum.UsernameLowercase),raw.ToLowerInvariant(), SelectedCondition),
            _                 => (SelectedItem.ToString(),           raw,                    SelectedCondition),
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
    
    protected async Task RunSearch()
    {
        ErrorMessage  = null;
        IsSearching   = true;
        HasSearched   = false;
        HasMore       = false;
        _lastId       = null;
        Results       = [];
        ResultColumns = [];
        TotalMatched  = null;
        _searchStart  = DateTime.UtcNow;
        _timer?.Dispose();
        _timer = new Timer(_ =>
        {
            Elapsed = DateTime.UtcNow - _searchStart;
            InvokeAsync(StateHasChanged);
        }, null, 0, 1000);

        try
        {
            if (SelectedItem == ItemEnum.Timestamp)
            {
                var date = DateTime.SpecifyKind(DatePart!.Value.Date, DateTimeKind.Utc);
                if (TimePart.HasValue)
                {
                    var utc = date + TimePart.Value.ToTimeSpan();
                    _tsFrom = utc;
                    _tsTo   = utc.AddSeconds(1); // exact match window
                }
                else
                {
                    _tsFrom = date;
                    _tsTo   = date.AddDays(1);
                }

                if (TimePart.HasValue)
                {
                    var utc = date + TimePart.Value.ToTimeSpan();
                    var docs = await MongoDbRepository.SearchIdentityByDateTime(
                        nameof(ItemEnum.Timestamp), utc, afterId: null, limit: PageSize);
                    await ApplyPageAsync(docs);
                    HasSearched = true;
                    IsSearching = false;
                    await InvokeAsync(StateHasChanged);
                    TotalMatched = await MongoDbRepository.CountIdentitiesByDateTime(
                        nameof(ItemEnum.Timestamp), utc);
                }
                else
                {
                    var docs = await MongoDbRepository.SearchIdentityByDateRange(
                        nameof(ItemEnum.Timestamp), date, date.AddDays(1), afterId: null, limit: PageSize);
                    await ApplyPageAsync(docs);
                    HasSearched = true;
                    IsSearching = false;
                    await InvokeAsync(StateHasChanged);
                    TotalMatched = await MongoDbRepository.CountIdentitiesByDateRange(
                        nameof(ItemEnum.Timestamp), date, date.AddDays(1));
                }
            }
            else
            {
                var (field, value, condition) = ResolveQuery();
                var docs = await MongoDbRepository.SearchIdentity(field, condition, value, afterId: null, limit: PageSize);
                await ApplyPageAsync(docs);
                HasSearched = true;
                IsSearching = false;
                await InvokeAsync(StateHasChanged);
                TotalMatched = await MongoDbRepository.CountIdentities(field, condition, value);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Search failed: {ex.Message}";
        }
        finally
        {
            _timer?.Dispose();
            _timer = null;
            Elapsed = DateTime.UtcNow - _searchStart;
            IsSearching = false;
            await InvokeAsync(StateHasChanged);
        }
    }
    
    protected async Task LoadMore()
    {
        if (!HasMore || IsLoadingMore)
            return;
        
        IsLoadingMore = true;
        ErrorMessage  = null;
        
        try
        {
            if (SelectedItem == ItemEnum.Timestamp)
            {
                var date = DateTime.SpecifyKind(DatePart!.Value.Date, DateTimeKind.Utc);

                if (TimePart.HasValue)
                {
                    var utc = date + TimePart.Value.ToTimeSpan();
                    var docs = await MongoDbRepository.SearchIdentityByDateTime(
                        nameof(ItemEnum.Timestamp), utc, afterId: _lastId, limit: PageSize);
                    await ApplyPageAsync(docs);
                }
                else
                {
                    var docs = await MongoDbRepository.SearchIdentityByDateRange(
                        nameof(ItemEnum.Timestamp), date, date.AddDays(1), afterId: _lastId, limit: PageSize);
                    await ApplyPageAsync(docs);
                }
            }
            else
            {
                var (field, value, condition) = ResolveQuery();
                var docs = await MongoDbRepository.SearchIdentity(field, condition, value, afterId: _lastId, limit: PageSize);
                await ApplyPageAsync(docs);
            }
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
    
    private async Task ApplyPageAsync(List<BsonDocument> docs)
    {
        if (docs.Count == 0)
        {
            HasMore = false;
            return;
        }
        
        _lastId = docs[^1]["_id"].AsObjectId;
        HasMore = docs.Count == PageSize;

        var uncached = docs
            .Where(d => d.Contains("ParseId"))
            .Select(d => d["ParseId"].AsObjectId)
            .Where(id => !_sourceNameCache.ContainsKey(id))
            .ToHashSet();

        await Task.WhenAll(uncached.Select(async id =>
        {
            var detail = await DashboardService.GetParseByIdAsync(id.ToString());
            _sourceNameCache[id] = detail?.FileName ?? id.ToString();
            _sourcePathCache[id] = detail?.SourcePath ?? id.ToString();
        }));

        if (!ResultColumns.Contains(SourceColumn))
            ResultColumns.Insert(0, SourceColumn);

        var newCols = SearchableItems
            .Select(i => i.ToString())
            .Where(name => docs.Any(d => d.Contains(name)) && !ResultColumns.Contains(name));
        ResultColumns.AddRange(newCols);

        foreach (var doc in docs)
        {
            var row = new Dictionary<string, string?>();

            if (doc.TryGetValue("ParseId", out var parseIdVal))
            {
                var parseId = parseIdVal.AsObjectId;
                row[SourceColumn]            = _sourceNameCache.TryGetValue(parseId, out var name) ? name : parseId.ToString();
                row[SourceColumn + "_path"]  = _sourcePathCache.TryGetValue(parseId, out var path) ? path : parseId.ToString();
                row[SourceColumn + "_navid"] = parseId.ToString();
            }
            else
            {
                row[SourceColumn]            = "-";
                row[SourceColumn + "_path"]  = null;
                row[SourceColumn + "_navid"] = null;
            }
            
            foreach (var col in ResultColumns)
            {
                if (col == SourceColumn) continue;
                if (!doc.TryGetValue(col, out var bVal)) continue;

                row[col] = col == nameof(ItemEnum.Hash)
                    ? StringFormatter.FormatHashes(bVal.AsBsonArray)
                    : col == nameof(ItemEnum.Timestamp)
                        ? bVal switch
                        {
                            BsonArray arr => string.Join(", ", arr
                                .Select(v => BsonTypeMapper.MapToDotNetValue(v) as DateTime?)
                                .Where(dt => dt.HasValue
                                             && (_tsFrom == null || dt.Value >= _tsFrom)
                                             && (_tsTo   == null || dt.Value <  _tsTo))
                                .Select(dt => dt!.Value.ToString("yyyy-MM-dd HH:mm:ss"))),
                            _ => BsonTypeMapper.MapToDotNetValue(bVal) is DateTime ts
                                ? ts.ToString("yyyy-MM-dd HH:mm:ss")
                                : bVal.ToString()
                        }
                        : bVal switch
                        {
                            BsonArray arr => string.Join(", ", arr.Select(v => v.ToString())),
                            _             => bVal.ToString()
                        };
            }
            
            Results.Add(row);
        }
    }
    
    protected async Task OpenParse(string? navId)
    {
        if (navId is not null)
            await Js.InvokeVoidAsync("open", $"/parse/{navId}", "_blank");
    }
}