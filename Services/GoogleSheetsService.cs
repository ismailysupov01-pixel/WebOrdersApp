using Google.Apis.Auth.OAuth2;
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
        // Получаем JSON ключ из переменной окружения
        var jsonKey = Environment.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT_JSON");

        GoogleCredential credential;

        if (!string.IsNullOrEmpty(jsonKey))
        {
            // Из переменной окружения (для Render)
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonKey));
            credential = GoogleCredential
                .FromStream(stream)
                .CreateScoped(SheetsService.Scope.Spreadsheets);
        }
        else
        {
            // Из локального файла (для разработки)
            var keyPath = _config["GoogleServiceAccountKeyPath"] ?? "service-account.json";
            using var stream = new FileStream(keyPath, FileMode.Open, FileAccess.Read);
            credential = GoogleCredential
                .FromStream(stream)
                .CreateScoped(SheetsService.Scope.Spreadsheets);
        }

        _service = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "WebOrdersApp"
        });

        await Task.CompletedTask;
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        var range    = $"{_sheetName}!A2:I";
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
                Executor    = Get(7),
                Author      = Get(8)
            });
            rowIndex++;
        }

        // Сортируем по дате по возрастанию
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
                new List<object> { order.Address, order.Phone, order.Amount, order.Date, OrderStatus.New, order.Comments, link, "", order.Author }
            }
        };
        var req = _service!.Spreadsheets.Values.Append(body, _spreadsheetId, $"{_sheetName}!A:I");
        req.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
        await req.ExecuteAsync();
    }

    public async Task UpdateOrderAsync(int rowIndex, string address, string phone, string amount,
        string date, string status, string comments, string executorName = "", string author = "")
    {
        var link  = Build2GisLink(address);
        var range = $"{_sheetName}!A{rowIndex}:I{rowIndex}";
        var body  = new ValueRange
        {
            Values = new List<IList<object>>
            {
                new List<object> { address, phone, amount, date, status, comments, link, executorName, author }
            }
        };
        var req = _service!.Spreadsheets.Values.Update(body, _spreadsheetId, range);
        req.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
        await req.ExecuteAsync();
    }

    public async Task UpdateStatusAsync(int rowIndex, string newStatus, string executorName = "", string newDate = "")
    {
        // Обновляем только статус (E) — не трогаем Comments (F) и TwoGisLink (G)
        var statusRange = $"{_sheetName}!E{rowIndex}";
        var statusBody = new ValueRange
        {
            Values = new List<IList<object>> { new List<object> { newStatus } }
        };
        var statusReq = _service!.Spreadsheets.Values.Update(statusBody, _spreadsheetId, statusRange);
        statusReq.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
        await statusReq.ExecuteAsync();

        // Обновляем исполнителя (H), если указан
        if (!string.IsNullOrEmpty(executorName))
        {
            var execRange = $"{_sheetName}!H{rowIndex}";
            var execBody = new ValueRange
            {
                Values = new List<IList<object>> { new List<object> { executorName } }
            };
            var execReq = _service!.Spreadsheets.Values.Update(execBody, _spreadsheetId, execRange);
            execReq.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await execReq.ExecuteAsync();
        }

        // Обновляем дату (D), если указана
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
