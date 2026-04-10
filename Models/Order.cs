namespace WebOrdersApp.Models;

public class Order
{
    public int RowIndex { get; set; }
    public int OrderNumber { get; set; }
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Amount { get; set; } = "";
    public string Date { get; set; } = "";
    public string Comments { get; set; } = "";
    public string TwoGisLink { get; set; } = "";
    public string Executor { get; set; } = "";
    public string Status { get; set; } = OrderStatus.New;
}

public static class OrderStatus
{
    public const string New       = "Невыполнена";
    public const string Completed = "Выполнена";
    public const string Cancelled = "Отменена";
}
