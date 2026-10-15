using Chugim.Domain.Enums;

namespace Chugim.Application.DTOs.Auth;

public sealed record AuthResponse
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
    public int UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
}
