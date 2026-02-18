using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Domain.Entities;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Infrastructure.Identity;

/// <summary>
/// Servicio para generación de tokens JWT y refresh tokens.
/// </summary>
/// <remarks>
/// Utiliza configuración desde appsettings.json en la sección "Jwt":
/// - Secret: clave secreta para firmar JWT (min 32 caracteres)
/// - Issuer: emisor del token
/// - Audience: audiencia del token
/// - AccessTokenExpirationInSeconds: duración del JWT (default 86400 = 24h)
/// - RefreshTokenExpirationInDays: duración del refresh token (default 7 días)
/// </remarks>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Inicializa el servicio con la configuración de la aplicación.
    /// </summary>
    /// <param name="configuration">Configuración para acceder a settings de JWT.</param>
    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Genera un token JWT de acceso para el usuario especificado.
    /// </summary>
    /// <param name="user">Usuario para el cual generar el token.</param>
    /// <returns>Token JWT firmado como string.</returns>
    /// <remarks>
    /// Claims incluidos en el JWT:
    /// - sub: UserId (Guid)
    /// - email: Email del usuario
    /// - jti: ID único del token (Guid)
    /// - role: Rol del usuario (ADMIN, SUPERVISOR, BECA, NONE)
    /// - firstName: Nombre del usuario
    /// - lastName: Apellido del usuario
    /// </remarks>
    public string GenerateAccessToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var roleName = user.Role switch
        {
            UserRole.Admin => "ADMIN",
            UserRole.Supervisor => "SUPERVISOR",
            UserRole.Beca => "BECA",
            _ => "NONE"
        };

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, roleName),
            new("firstName", user.FirstName),
            new("lastName", user.LastName)
        };

        var expirationSeconds = GetAccessTokenExpirationInSeconds();
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(expirationSeconds),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Genera un refresh token criptográficamente seguro.
    /// </summary>
    /// <returns>String aleatorio base64 de 64 bytes (512 bits de entropía).</returns>
    /// <remarks>
    /// Usa RandomNumberGenerator para generar bytes aleatorios criptográficamente seguros.
    /// </remarks>
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Obtiene el tiempo de expiración del access token en segundos desde configuración.
    /// </summary>
    /// <returns>Segundos hasta la expiración (default 86400 = 24 horas).</returns>
    public int GetAccessTokenExpirationInSeconds()
    {
        var value = _configuration.GetSection("Jwt")["AccessTokenExpirationInSeconds"];
        return int.TryParse(value, out var seconds) ? seconds : 86400; // Default 24h
    }

    /// <summary>
    /// Obtiene el tiempo de expiración del refresh token en días desde configuración.
    /// </summary>
    /// <returns>Días hasta la expiración (default 7 días).</returns>
    public int GetRefreshTokenExpirationInDays()
    {
        var value = _configuration.GetSection("Jwt")["RefreshTokenExpirationInDays"];
        return int.TryParse(value, out var days) ? days : 7; // Default 7 days
    }
}
