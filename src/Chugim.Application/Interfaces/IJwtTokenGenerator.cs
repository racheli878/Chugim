using Chugim.Domain.Entities;

namespace Chugim.Application.Interfaces;

public interface IJwtTokenGenerator
{
    AccessToken Generate(User user);
}

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);
