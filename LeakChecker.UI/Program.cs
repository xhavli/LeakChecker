using LeakChecker.UI.Services;
using LeakChecker.DataParser.Database;
using LeakChecker.DataParser.Database.Facade;
using LeakChecker.UI.Components;
using MongoDB.Bson;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient("mongodb://localhost:27017"));
builder.Services.AddSingleton<IMongoDatabase>(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase("LeakCheckerDb"));
builder.Services.AddSingleton<IDatabase, MongoDbFacade>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

// pre-warm MongoDB connection so first dashboard load isn't slow
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
    await db.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();