using PayDefteri.Application.Auth;
using PayDefteri.Application.Common.Interfaces;
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
    public sealed record LoginRequest(string Email, string Password, bool RememberMe = false);
    public sealed record UpdateProfileRequest(string DisplayName);
    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
    public sealed record DeleteAccountRequest(string CurrentPassword);
    public sealed record ConfirmEmailRequest(string UserId, string Token);

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
        var result = await _sender.Send(new LoginCommand(request.Email, request.Password, request.RememberMe), ct);
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

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] ConfirmEmailRequest request, CancellationToken ct)
    {
        await _sender.Send(new ConfirmEmailCommand(request.UserId, request.Token), ct);
        return NoContent();
    }

    [Authorize]
    [EnableRateLimiting("auth")]
    [HttpPost("verify-email/resend")]
    public async Task<ActionResult<EmailVerificationResult>> ResendVerification(CancellationToken ct)
        => Ok(await _sender.Send(new ResendEmailVerificationCommand(), ct));

    [Authorize]
    [EnableRateLimiting("auth")]
    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request, CancellationToken ct)
    {
        await _sender.Send(new DeleteAccountCommand(request.CurrentPassword), ct);
        ClearSessionCookies();
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        ClearSessionCookies();
        return NoContent();
    }

    private void ClearSessionCookies()
    {
        var cookie = BrowserCookie(httpOnly: false);
        Response.Cookies.Delete("paydefteri_session", cookie);
        Response.Cookies.Delete("paydefteri_xsrf", cookie);
        Response.Cookies.Delete("paydefteri_antiforgery", cookie);
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
