using WebOrdersApp.Components;
using WebOrdersApp.Models;
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

// ===== TILDA WEBHOOK =====
// Принимает POST /api/orders?key=SECRET от формы Tilda
app.MapPost("/api/orders", async (HttpRequest request, GoogleSheetsService sheetsService) =>
{
    // Проверка секретного ключа
    var expectedKey = Environment.GetEnvironmentVariable("TILDA_WEBHOOK_SECRET") ?? "";
    var providedKey = request.Query["key"].FirstOrDefault() ?? "";
    if (!string.IsNullOrEmpty(expectedKey) && providedKey != expectedKey)
        return Results.Json(new { success = false, error = "Unauthorized" }, statusCode: 401);

    string address = "", phone = "", amount = "", date = "", comments = "", author = "Tilda";

    var ct = request.ContentType ?? "";
    if (ct.Contains("application/json"))
    {
        var body = await request.ReadFromJsonAsync<Dictionary<string, string>>();
        if (body != null)
        {
            body.TryGetValue("address",  out address!);
            body.TryGetValue("phone",    out phone!);
            body.TryGetValue("amount",   out amount!);
            body.TryGetValue("date",     out date!);
            body.TryGetValue("comments", out comments!);
            body.TryGetValue("author",   out author!);
        }
    }
    else
    {
        // Tilda отправляет application/x-www-form-urlencoded
        var form = await request.ReadFormAsync();

        // Tilda проверяет webhook тестовым запросом test=test — сразу отвечаем OK
        if (form.ContainsKey("test"))
            return Results.Json(new { success = true });

        address  = form["address"].FirstOrDefault()  ?? "";
        phone    = form["phone"].FirstOrDefault()    ?? "";
        amount   = form["amount"].FirstOrDefault()   ?? "";
        date     = form["date"].FirstOrDefault()     ?? "";
        comments = form["comments"].FirstOrDefault() ?? "";
        author   = form["author"].FirstOrDefault()   ?? "Tilda";
    }

    if (string.IsNullOrWhiteSpace(address))
        return Results.Json(new { success = false, error = "address is required" }, statusCode: 400);

    // Нормализуем дату → dd.MM.yyyy
    if (string.IsNullOrWhiteSpace(date))
    {
        date = DateTime.Today.ToString("dd.MM.yyyy");
    }
    else
    {
        string[] formats = { "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "d/M/yyyy" };
        if (DateTime.TryParseExact(date, formats,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var parsedDate))
            date = parsedDate.ToString("dd.MM.yyyy");
    }

    var order = new Order
    {
        Address  = address.Trim(),
        Phone    = phone?.Trim()    ?? "",
        Amount   = amount?.Trim()   ?? "",
        Date     = date,
        Comments = comments?.Trim() ?? "",
        Author   = string.IsNullOrWhiteSpace(author) ? "Tilda" : author.Trim()
    };

    await sheetsService.AddOrderAsync(order);
    return Results.Json(new { success = true });
}).ExcludeFromDescription();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
