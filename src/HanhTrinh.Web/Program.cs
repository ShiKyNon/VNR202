using HanhTrinh.Web.Components;
using HanhTrinh.Web.Game;
using HanhTrinh.Web.Services;
using HanhTrinh.Web.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton(ContentStore.Load(Path.Combine(AppContext.BaseDirectory, "Data")));
builder.Services.AddSingleton<SeenRegistry>();
builder.Services.AddSingleton<RoomManager>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<RoomManager>());
builder.Services.AddScoped<PlayerSession>();
builder.Services.AddDbContextFactory<AppDb>(o => DbSetup.Configure(o, builder.Configuration.GetConnectionString("Default")));
builder.Services.AddSingleton<MatchArchive>();
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear(); o.KnownProxies.Clear();
});

var app = builder.Build();

if (app.Environment.IsDevelopment() && Environment.GetEnvironmentVariable("HT_FAST") == "1") Match.Cooldown = TimeSpan.FromMilliseconds(300);

// shortcut: EnsureCreated, no schema upgrades — switch to per-provider migrations when the schema changes
using (var db = app.Services.GetRequiredService<IDbContextFactory<AppDb>>().CreateDbContext()) db.Database.EnsureCreated();

var archive = app.Services.GetRequiredService<MatchArchive>();
app.Services.GetRequiredService<RoomManager>().MatchEnded += (room, match) => _ = Task.Run(async () =>
{
    try
    {
        var id = await archive.SaveAsync(room.Code, match);
        lock (app.Services.GetRequiredService<RoomManager>().Gate) room.LastMatchId = id;
    }
    catch (Exception ex) { app.Logger.LogError(ex, "Không lưu được kết quả trận phòng {Code}", room.Code); }
});

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/healthz", () => "ok");
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
