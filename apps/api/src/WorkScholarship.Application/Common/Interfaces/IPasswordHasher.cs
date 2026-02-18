namespace WorkScholarship.Application.Common.Interfaces;

/// <summary>
/// Servicio para hashear y verificar contraseñas de forma segura.
/// </summary>
/// <remarks>
/// Implementa wrapping de PasswordHasher de ASP.NET Identity con algoritmo PBKDF2.
/// </remarks>
public interface IPasswordHasher
{
    /// <summary>
    /// Genera un hash seguro de la contraseña proporcionada.
    /// </summary>
    /// <param name="password">Contraseña en texto plano a hashear.</param>
    /// <returns>Hash de la contraseña.</returns>
    string Hash(string password);

    /// <summary>
    /// Verifica si una contraseña coincide con un hash.
    /// </summary>
    /// <param name="password">Contraseña en texto plano a verificar.</param>
    /// <param name="hash">Hash almacenado con el que comparar.</param>
    /// <returns>True si la contraseña es correcta; false en caso contrario.</returns>
    bool Verify(string password, string hash);
}
