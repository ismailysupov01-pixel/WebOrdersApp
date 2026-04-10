namespace WebOrdersApp.Services;

public class UserSession
{
    public bool IsPasswordVerified { get; set; } = false;
    public string Role { get; set; } = "";       // "Оператор" or "Исполнитель"
    public string Name { get; set; } = "";        // ФИО пользователя
    public string Login { get; set; } = "";       // логин

    public bool IsAuthenticated => IsPasswordVerified && !string.IsNullOrEmpty(Role);
}
