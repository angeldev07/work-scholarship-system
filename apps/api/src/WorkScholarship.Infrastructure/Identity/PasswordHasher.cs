using Microsoft.AspNetCore.Identity;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Domain.Entities;

namespace WorkScholarship.Infrastructure.Identity;

/// <summary>
/// Servicio para hashear y verificar contraseñas usando ASP.NET Identity PasswordHasher.
/// </summary>
/// <remarks>
/// Utiliza el algoritmo PBKDF2 de ASP.NET Identity con:
/// - Salt aleatorio único por contraseña
/// - 10,000 iteraciones (default de Identity)
/// - 256 bits de longitud de subclave
/// Wrapper sobre PasswordHasher&lt;TUser&gt; para compatibilidad con la interfaz de Application.
/// </remarks>
public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _innerHasher = new();

    // Instancia estática dummy usada como parámetro genérico para el PasswordHasher de Identity.
    // El PasswordHasher<TUser> de Identity requiere una instancia de TUser pero no la usa
    // para operaciones de hash/verify - solo usa el tipo genérico para normalización.
    private static readonly User DummyUser = null!;

    /// <summary>
    /// Genera un hash seguro de la contraseña proporcionada.
    /// </summary>
    /// <param name="password">Contraseña en texto plano a hashear.</param>
    /// <returns>Hash de la contraseña con salt incluido.</returns>
    public string Hash(string password)
    {
        return _innerHasher.HashPassword(DummyUser, password);
    }

    /// <summary>
    /// Verifica si una contraseña coincide con un hash almacenado.
    /// </summary>
    /// <param name="password">Contraseña en texto plano a verificar.</param>
    /// <param name="hash">Hash almacenado con el que comparar.</param>
    /// <returns>
    /// True si la contraseña es correcta;
    /// false en caso contrario.
    /// </returns>
    /// <remarks>
    /// Acepta tanto Success como SuccessRehashNeeded (si el algoritmo de hash fue actualizado).
    /// </remarks>
    public bool Verify(string password, string hash)
    {
        var result = _innerHasher.VerifyHashedPassword(DummyUser, hash, password);
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
