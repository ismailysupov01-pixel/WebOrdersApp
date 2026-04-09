namespace WebOrdersApp.Services;

public class UserSession
{
    public string Role { get; set; } = ""; // "Оператор" or "Исполнитель"
    public bool IsAuthenticated => !string.IsNullOrEmpty(Role);
}
