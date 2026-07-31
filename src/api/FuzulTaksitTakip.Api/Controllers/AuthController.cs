using FuzulTaksitTakip.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuzulTaksitTakip.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    public sealed record RegisterRequest(string Email, string Password, string DisplayName);
    public sealed record LoginRequest(string Email, string Password);

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new RegisterCommand(request.Email, request.Password, request.DisplayName), ct);
        return Created(string.Empty, result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResult>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new LoginCommand(request.Email, request.Password), ct);
        return Ok(result);
    }
}
