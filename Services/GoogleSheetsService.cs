using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using WebOrdersApp.Models;

namespace WebOrdersApp.Services;

public class GoogleSheetsService
{
    private SheetsService? _service;
    private readonly string _spreadsheetId;
    private readonly string _sheetName;
    private readonly IConfiguration _config;

    public GoogleSheetsService(IConfiguration config)
    {
        _config = config;
        _spreadsheetId = config["SpreadsheetId"] ?? "";
        _sheetName = config["SheetName"] ?? "Лист1";
    }

    public async Task InitAsync()
    {
        var clientId     = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")     ?? _config["GoogleOAuth:ClientId"]     ?? "";
        var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? _config["GoogleOAuth:ClientSecret"] ?? "";
        var refreshToken = Environment.GetEnvironmentVariable("GOOGLE_REFRESH_TOKEN") ?? _config["GoogleOAuth:RefreshToken"] ?? "";

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
            Scopes = new[] { SheetsService.Scope.Spreadsheets }
        });

        var credential = new UserCredential(flow, "user", new TokenResponse { RefreshToken = refreshToken });

        _service = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "WebOrdersApp"
        });
        await Task.CompletedTask;
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        var range    = $"{_sheetName}!A2:H";
        var response = await _service!.Spreadsheets.Values.Get(_spreadsheetId, range).ExecuteAsync();
        var orders   = new List<Order>();

        if (response.Values == null) return orders;

        int rowIndex = 2;
        foreach (var row in response.Values)
        {
            string Get(int i) => row.Count > i ? row[i]?.ToString() ?? "" : "";
            orders.Add(new Order
            {
                RowIndex    = rowIndex,
                OrderNumber = rowIndex - 1,
                Address     = Get(0),
                Phone       = Get(1),
                Amount      = Get(2),
                Date        = Get(3),
                Status      = string.IsNullOrEmpty(Get(4)) ? OrderStatus.New : Get(4),
                Comments    = Get(5),
                TwoGisLink  = Get(6),
                Executor    = Get(7)
            });
            rowIndex++;
        }

        // Сортируем по дате по возрастанию (старые сверху, новые снизу)
        orders.Sort((a, b) =>
        {
            bool da = TryParseDate(a.Date, out var dtA);
            bool db = TryParseDate(b.Date, out var dtB);
            if (da && db) return dtA.CompareTo(dtB);
            if (da) return -1;
            if (db) return 1;
            return 0;
        });

        // Присваиваем порядковые номера после сортировки
        for (int i = 0; i < orders.Count; i++)
            orders[i].OrderNumber = i + 1;

        return orders;
    }

    public async Task AddOrderAsync(Order order)
    {
        var link = Build2GisLink(order.Address);
        var body = new ValueRange
        {
            Values = new List<IList<object>>
            {
                new List<object> { order.Address, order.Phone, order.Amount, order.Date, OrderStatus.New, order.Comments, link, "" }
            }
        };
        var req = _service!.Spreadsheets.Values.Append(body, _spreadsheetId, $"{_sheetName}!A:H");
        req.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
        await req.ExecuteAsync();
    }

    // Обновляет статус, ФИО исполнителя и (опционально) дату
    public async Task UpdateStatusAsync(int rowIndex, string newStatus, string executorName = "", string newDate = "")
    {
        // E = статус, H = исполнитель
        var range = $"{_sheetName}!E{rowIndex}:H{rowIndex}";
        var body = new ValueRange
        {
            Values = new List<IList<object>>
            {
                new List<object> { newStatus, "", "", executorName }
            }
        };
        var req = _service!.Spreadsheets.Values.Update(body, _spreadsheetId, range);
        req.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
        await req.ExecuteAsync();

        // Обновляем дату в столбце D, если она изменилась
        if (!string.IsNullOrEmpty(newDate))
        {
            var dateRange = $"{_sheetName}!D{rowIndex}";
            var dateBody = new ValueRange
            {
                Values = new List<IList<object>> { new List<object> { newDate } }
            };
            var dateReq = _service!.Spreadsheets.Values.Update(dateBody, _spreadsheetId, dateRange);
            dateReq.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await dateReq.ExecuteAsync();
        }
    }

    public static string Build2GisLink(string address) =>
        $"https://2gis.kz/almaty/search/{Uri.EscapeDataString(address)}";

    static bool TryParseDate(string s, out DateTime dt)
    {
        return DateTime.TryParseExact(s, "dd.MM.yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out dt);
    }
}
