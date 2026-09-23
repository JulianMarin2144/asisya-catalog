namespace Asisya.Application.Auth;

public sealed class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}
