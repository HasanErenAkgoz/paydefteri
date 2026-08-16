using PayDefteri.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PayDefteri.Infrastructure.Auth;
using Microsoft.AspNetCore.Antiforgery;

namespace PayDefteri.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IWebHostEnvironment _environment;
    private readonly IAntiforgery _antiforgery;

    public AuthController(ISender sender, IWebHostEnvironment environment, IAntiforgery antiforgery)
    {
        _sender = sender;
        _environment = environment;
        _antiforgery = antiforgery;
    }

    public sealed record RegisterRequest(string Email, string Password, string DisplayName);
    public sealed record LoginRequest(string Email, string Password);
    public sealed record UpdateProfileRequest(string DisplayName);
    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status201Created)]
    public async Task<ActionResult<LoginResult>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new RegisterCommand(request.Email, request.Password, request.DisplayName), ct);
        SetSessionCookie(result);
        return Created(string.Empty, result);
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResult>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new LoginCommand(request.Email, request.Password), ct);
        SetSessionCookie(result);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("xsrf")]
    public IActionResult RefreshXsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        Response.Cookies.Append(
            "paydefteri_xsrf",
            tokens.RequestToken ?? throw new InvalidOperationException("XSRF request token could not be created."),
            BrowserCookie(httpOnly: false));
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> Me(CancellationToken ct)
        => Ok(await _sender.Send(new GetMyProfileQuery(), ct));

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<LoginResult>> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new UpdateProfileCommand(request.DisplayName), ct);
        SetSessionCookie(result);
        _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await _sender.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), ct);
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var cookie = BrowserCookie(httpOnly: false);
        Response.Cookies.Delete("paydefteri_session", cookie);
        Response.Cookies.Delete("paydefteri_xsrf", cookie);
        Response.Cookies.Delete("paydefteri_antiforgery", cookie);
        return NoContent();
    }

    private void SetSessionCookie(LoginResult result) => Response.Cookies.Append(
        "paydefteri_session",
        result.AccessToken,
        new CookieOptions
        {
            HttpOnly = true,
            Secure = _environment.IsProduction(),
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = new DateTimeOffset(result.ExpiresAt),
        });

    private CookieOptions BrowserCookie(bool httpOnly) => new()
    {
        HttpOnly = httpOnly,
        Secure = _environment.IsProduction(),
        SameSite = SameSiteMode.Lax,
        Path = "/",
    };
}
