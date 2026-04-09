namespace WebOrdersApp.Services;

public class UserSession
{
    public bool IsPasswordVerified { get; set; } = false;
    public string Role { get; set; } = "";
    public bool IsAuthenticated => IsPasswordVerified && !string.IsNullOrEmpty(Role);
}
