namespace Nugget.Web.Models;

/// <summary>
/// 認証済みユーザー情報
/// </summary>
public class AuthenticatedUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool IsAdmin => Role == "Admin";
}

/// <summary>
/// 認証状態
/// </summary>
public class AuthState
{
    public bool IsAuthenticated { get; set; }
    public AuthenticatedUser? User { get; set; }
}
