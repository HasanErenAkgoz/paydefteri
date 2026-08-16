using System.Security.Claims;
using FuzulTaksitTakip.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FuzulTaksitTakip.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

    public string? Email =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("email");

    public string? DisplayName =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("display_name")
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("name");

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public bool IsSuperAdmin
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null)
            {
                return false;
            }

            if (user.IsInRole("SuperAdmin"))
            {
                return true;
            }

            var flag = user.FindFirstValue("is_super_admin");
            return string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
