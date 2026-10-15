using Chugim.API.Authorization;
using Chugim.Application.DTOs.Auth;
using Chugim.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Chugim.API.Controllers;

[Route("api/auth")]
[EnableRateLimiting(RateLimitPolicies.Auth)]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>הרשמת משתתפת חדשה. מחזיר token, כך שהיא מחוברת מיד.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        if (!result.IsSuccess)
            return FromResult(result);

        // אין endpoint ציבורי לצפייה במשתמשת, ולכן 201 בלי Location.
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        FromResult(await _authService.LoginAsync(request, cancellationToken));
}
