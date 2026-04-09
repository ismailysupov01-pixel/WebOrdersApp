using WebOrdersApp.Components;
using WebOrdersApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Google Sheets - singleton, initialized once
builder.Services.AddSingleton<GoogleSheetsService>();

// User session - scoped (per Blazor circuit/tab)
builder.Services.AddScoped<UserSession>();

var app = builder.Build();

// Initialize Google Sheets on startup
var sheets = app.Services.GetRequiredService<GoogleSheetsService>();
await sheets.InitAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
