using HanhTrinh.Web.Components;
using HanhTrinh.Web.Game;
using HanhTrinh.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton(ContentStore.Load(Path.Combine(AppContext.BaseDirectory, "Data")));
builder.Services.AddSingleton<SeenRegistry>();
builder.Services.AddSingleton<RoomManager>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<RoomManager>());
builder.Services.AddScoped<PlayerSession>();
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear(); o.KnownProxies.Clear();
});

var app = builder.Build();

if (app.Environment.IsDevelopment() && Environment.GetEnvironmentVariable("HT_FAST") == "1") Match.Cooldown = TimeSpan.FromMilliseconds(300);

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
