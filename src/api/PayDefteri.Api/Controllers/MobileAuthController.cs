using PayDefteri.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace PayDefteri.Api.Controllers;

[ApiController]
[Route("api/mobile/v1/auth")]
public sealed class MobileAuthController : ControllerBase
{
    private readonly ISender _sender;

    public MobileAuthController(ISender sender)
    {
        _sender = sender;
    }

    public sealed record DeviceRequest(string DeviceName, string Platform, string AppVersion);
    public sealed record LoginRequest(string Email, string Password, DeviceRequest Device);
    public sealed record RegisterRequest(string Email, string Password, string DisplayName, DeviceRequest Device);
    public sealed record RefreshRequest(string RefreshToken);

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<ActionResult<MobileAuthResult>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await _sender.Send(new MobileLoginCommand(
            request.Email,
            request.Password,
            ToDevice(request.Device)), ct));

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    public async Task<ActionResult<MobileAuthResult>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new MobileRegisterCommand(
            request.Email,
            request.Password,
            request.DisplayName,
            ToDevice(request.Device)), ct);
        return Created(string.Empty, result);
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    public async Task<ActionResult<MobileAuthResult>> Refresh(RefreshRequest request, CancellationToken ct) =>
        Ok(await _sender.Send(new RefreshMobileSessionCommand(request.RefreshToken), ct));

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        await _sender.Send(new LogoutMobileSessionCommand(request.RefreshToken), ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("sessions")]
    public async Task<ActionResult<IReadOnlyList<MobileSessionDto>>> Sessions(CancellationToken ct)
    {
        Guid? currentSessionId = Request.Headers.TryGetValue("X-Mobile-Session-Id", out var value)
            && Guid.TryParse(value, out var parsed)
                ? parsed
                : null;
        return Ok(await _sender.Send(new ListMobileSessionsQuery(currentSessionId), ct));
    }

    [Authorize]
    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
    {
        await _sender.Send(new RevokeMobileSessionCommand(sessionId), ct);
        return NoContent();
    }

    private static MobileDeviceInfo ToDevice(DeviceRequest request) =>
        new(request.DeviceName, request.Platform, request.AppVersion);
}
