using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WorkScholarship.Application.Common.Interfaces;

namespace WorkScholarship.Infrastructure.Identity;

/// <summary>
/// Servicio para acceder a información del usuario actualmente autenticado desde los claims del JWT.
/// </summary>
/// <remarks>
/// Extrae datos del ClaimsPrincipal del HttpContext actual.
/// Usa IHttpContextAccessor para acceder al contexto HTTP en servicios no-controller.
/// Soporta tanto claims estándar de JWT como claims de ASP.NET ClaimsIdentity (fallback).
/// </remarks>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Inicializa el servicio con el accessor del contexto HTTP.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor para obtener el HttpContext actual.</param>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Obtiene el identificador único del usuario autenticado desde los claims del JWT.
    /// </summary>
    /// <value>
    /// Guid del usuario extraído del claim "sub" (JwtRegisteredClaimNames.Sub)
    /// o ClaimTypes.NameIdentifier como fallback. Null si no hay usuario autenticado.
    /// </value>
    public Guid? UserId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    /// <summary>
    /// Obtiene el email del usuario autenticado desde los claims del JWT.
    /// </summary>
    /// <value>
    /// Email extraído del claim "email" (JwtRegisteredClaimNames.Email)
    /// o ClaimTypes.Email como fallback. Null si no hay usuario autenticado.
    /// </value>
    public string? Email =>
        _httpContextAccessor.HttpContext?.User
            .FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.Email);

    /// <summary>
    /// Obtiene el rol del usuario autenticado desde los claims del JWT.
    /// </summary>
    /// <value>
    /// Rol como string (ADMIN, SUPERVISOR, BECA, NONE) extraído del claim ClaimTypes.Role.
    /// Null si no hay usuario autenticado.
    /// </value>
    public string? Role =>
        _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.Role);

    /// <summary>
    /// Indica si hay un usuario autenticado en la petición actual.
    /// </summary>
    /// <value>
    /// True si el usuario está autenticado (JWT válido presente);
    /// false en caso contrario.
    /// </value>
    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
