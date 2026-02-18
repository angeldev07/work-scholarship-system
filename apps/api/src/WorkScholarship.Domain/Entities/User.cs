using WorkScholarship.Domain.Common;
using WorkScholarship.Domain.Enums;

namespace WorkScholarship.Domain.Entities;

/// <summary>
/// Representa un usuario del sistema de becas trabajo.
/// Entidad rica con comportamiento de dominio para gestión de autenticación, roles y tokens.
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Número máximo de intentos fallidos de login antes de bloquear la cuenta.
    /// </summary>
    public const int MAX_FAILED_LOGIN_ATTEMPTS = 5;

    /// <summary>
    /// Duración del bloqueo de cuenta en minutos tras exceder intentos fallidos.
    /// </summary>
    public const int LOCKOUT_DURATION_MINUTES = 15;

    private User() { }

    /// <summary>
    /// Crea un nuevo usuario con autenticación local (email y contraseña).
    /// </summary>
    /// <param name="email">Dirección de correo electrónico del usuario.</param>
    /// <param name="firstName">Nombre del usuario.</param>
    /// <param name="lastName">Apellido del usuario.</param>
    /// <param name="passwordHash">Hash de la contraseña del usuario.</param>
    /// <param name="role">Rol asignado al usuario.</param>
    /// <param name="createdBy">Identificador del usuario que crea este registro.</param>
    /// <returns>Nueva instancia de usuario.</returns>
    /// <exception cref="ArgumentException">Si algún parámetro requerido está vacío o es nulo.</exception>
    public static User Create(
        string email,
        string firstName,
        string lastName,
        string passwordHash,
        UserRole role,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        return new User
        {
            Email = email.Trim().ToLowerInvariant(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            PasswordHash = passwordHash,
            Role = role,
            AuthProvider = AuthProvider.Local,
            IsActive = true,
            FailedLoginAttempts = 0,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Crea un nuevo usuario con autenticación OAuth de Google.
    /// </summary>
    /// <param name="email">Dirección de correo electrónico institucional del usuario.</param>
    /// <param name="firstName">Nombre del usuario obtenido de Google.</param>
    /// <param name="lastName">Apellido del usuario obtenido de Google.</param>
    /// <param name="googleId">Identificador único de Google del usuario.</param>
    /// <param name="photoUrl">URL de la foto de perfil de Google.</param>
    /// <param name="createdBy">Identificador del usuario que crea este registro.</param>
    /// <returns>Nueva instancia de usuario autenticado por Google.</returns>
    /// <exception cref="ArgumentException">Si email, firstName o lastName están vacíos o son nulos.</exception>
    /// <remarks>
    /// Los usuarios de Google no tienen contraseña local y se crean con rol None inicialmente.
    /// </remarks>
    public static User CreateFromGoogle(
        string email,
        string firstName,
        string lastName,
        string? googleId,
        string? photoUrl,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));

        return new User
        {
            Email = email.Trim().ToLowerInvariant(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            PasswordHash = null,
            GoogleId = googleId,
            PhotoUrl = photoUrl,
            Role = UserRole.None,
            AuthProvider = AuthProvider.Google,
            IsActive = true,
            FailedLoginAttempts = 0,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Dirección de correo electrónico del usuario (única en el sistema).
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Nombre del usuario.
    /// </summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>
    /// Apellido del usuario.
    /// </summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>
    /// Nombre completo del usuario (combinación de FirstName y LastName).
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// Hash de la contraseña del usuario (solo para autenticación local).
    /// </summary>
    /// <remarks>
    /// Nulo para usuarios que usan Google OAuth.
    /// </remarks>
    public string? PasswordHash { get; private set; }

    /// <summary>
    /// Rol del usuario en el sistema.
    /// </summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// Proveedor de autenticación usado por el usuario.
    /// </summary>
    public AuthProvider AuthProvider { get; private set; }

    /// <summary>
    /// Indica si la cuenta del usuario está activa.
    /// </summary>
    /// <remarks>
    /// Las cuentas inactivas no pueden autenticarse.
    /// </remarks>
    public bool IsActive { get; private set; }

    /// <summary>
    /// URL de la foto de perfil del usuario.
    /// </summary>
    public string? PhotoUrl { get; private set; }

    /// <summary>
    /// Identificador único de Google del usuario (para OAuth).
    /// </summary>
    public string? GoogleId { get; private set; }

    /// <summary>
    /// Fecha y hora del último login exitoso.
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>
    /// Contador de intentos fallidos de login consecutivos.
    /// </summary>
    /// <remarks>
    /// Se resetea a 0 tras un login exitoso.
    /// La cuenta se bloquea al alcanzar MAX_FAILED_LOGIN_ATTEMPTS.
    /// </remarks>
    public int FailedLoginAttempts { get; private set; }

    /// <summary>
    /// Fecha y hora hasta la cual la cuenta está bloqueada.
    /// </summary>
    /// <remarks>
    /// Nulo si la cuenta no está bloqueada.
    /// </remarks>
    public DateTime? LockoutEndAt { get; private set; }

    /// <summary>
    /// Token para reseteo de contraseña.
    /// </summary>
    public string? PasswordResetToken { get; private set; }

    /// <summary>
    /// Fecha y hora de expiración del token de reseteo de contraseña.
    /// </summary>
    public DateTime? PasswordResetTokenExpiresAt { get; private set; }

    private readonly List<RefreshToken> _refreshTokens = [];

    /// <summary>
    /// Colección de tokens de actualización (refresh tokens) asociados al usuario.
    /// </summary>
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    /// <summary>
    /// Establece una nueva contraseña para el usuario.
    /// </summary>
    /// <param name="passwordHash">Nuevo hash de contraseña.</param>
    /// <exception cref="ArgumentException">Si el hash está vacío o es nulo.</exception>
    /// <remarks>
    /// Limpia cualquier token de reseteo de contraseña existente.
    /// </remarks>
    public void SetPassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
        PasswordResetToken = null;
        PasswordResetTokenExpiresAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cambia el rol del usuario.
    /// </summary>
    /// <param name="newRole">Nuevo rol a asignar.</param>
    /// <param name="updatedBy">Identificador del usuario que realiza el cambio.</param>
    /// <exception cref="ArgumentException">Si se intenta asignar el rol None.</exception>
    /// <remarks>
    /// No se permite asignar explícitamente el rol None.
    /// </remarks>
    public void ChangeRole(UserRole newRole, string updatedBy)
    {
        if (newRole == UserRole.None)
            throw new ArgumentException("Cannot assign None role.", nameof(newRole));

        Role = newRole;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Registra un login exitoso.
    /// </summary>
    /// <remarks>
    /// Actualiza la fecha del último login, resetea el contador de intentos fallidos
    /// y elimina cualquier bloqueo activo.
    /// </remarks>
    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockoutEndAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Registra un intento fallido de login.
    /// </summary>
    /// <remarks>
    /// Incrementa el contador de intentos fallidos.
    /// Si se alcanzan MAX_FAILED_LOGIN_ATTEMPTS, bloquea la cuenta por LOCKOUT_DURATION_MINUTES.
    /// </remarks>
    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= MAX_FAILED_LOGIN_ATTEMPTS)
        {
            LockoutEndAt = DateTime.UtcNow.AddMinutes(LOCKOUT_DURATION_MINUTES);
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Verifica si la cuenta del usuario está bloqueada.
    /// </summary>
    /// <returns>True si la cuenta está bloqueada; false en caso contrario.</returns>
    /// <remarks>
    /// Una cuenta está bloqueada si LockoutEndAt tiene valor y es mayor que la fecha actual.
    /// </remarks>
    public bool IsLockedOut()
    {
        return LockoutEndAt.HasValue && LockoutEndAt.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// Vincula una cuenta de Google al usuario.
    /// </summary>
    /// <param name="googleId">Identificador único de Google.</param>
    /// <param name="photoUrl">URL de la foto de perfil de Google (opcional).</param>
    /// <exception cref="ArgumentException">Si googleId está vacío o es nulo.</exception>
    /// <remarks>
    /// Cambia el proveedor de autenticación a Google.
    /// </remarks>
    public void LinkGoogleAccount(string googleId, string? photoUrl)
    {
        if (string.IsNullOrWhiteSpace(googleId))
            throw new ArgumentException("Google ID is required.", nameof(googleId));

        GoogleId = googleId;
        AuthProvider = AuthProvider.Google;

        if (!string.IsNullOrWhiteSpace(photoUrl))
            PhotoUrl = photoUrl;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Establece un token de reseteo de contraseña.
    /// </summary>
    /// <param name="token">Token generado para el reseteo.</param>
    /// <param name="expiresAt">Fecha y hora de expiración del token.</param>
    /// <exception cref="ArgumentException">Si el token está vacío o es nulo.</exception>
    public void SetPasswordResetToken(string token, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required.", nameof(token));

        PasswordResetToken = token;
        PasswordResetTokenExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Valida si un token de reseteo de contraseña es válido.
    /// </summary>
    /// <param name="token">Token a validar.</param>
    /// <returns>True si el token es válido y no ha expirado; false en caso contrario.</returns>
    public bool IsPasswordResetTokenValid(string token)
    {
        return PasswordResetToken == token
            && PasswordResetTokenExpiresAt.HasValue
            && PasswordResetTokenExpiresAt.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// Desactiva la cuenta del usuario.
    /// </summary>
    /// <param name="updatedBy">Identificador del usuario que realiza la desactivación.</param>
    /// <remarks>
    /// Las cuentas desactivadas no pueden autenticarse.
    /// </remarks>
    public void Deactivate(string updatedBy)
    {
        IsActive = false;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activa la cuenta del usuario.
    /// </summary>
    /// <param name="updatedBy">Identificador del usuario que realiza la activación.</param>
    public void Activate(string updatedBy)
    {
        IsActive = true;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Agrega un nuevo token de actualización al usuario.
    /// </summary>
    /// <param name="token">Valor del refresh token.</param>
    /// <param name="expiresAt">Fecha y hora de expiración del token.</param>
    /// <param name="ipAddress">Dirección IP desde donde se generó el token (opcional).</param>
    /// <returns>El refresh token creado.</returns>
    public RefreshToken AddRefreshToken(string token, DateTime expiresAt, string? ipAddress = null)
    {
        var refreshToken = RefreshToken.Create(Id, token, expiresAt, ipAddress);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    /// <summary>
    /// Revoca todos los tokens de actualización activos del usuario.
    /// </summary>
    /// <remarks>
    /// Útil al cambiar contraseña o cerrar sesión en todos los dispositivos.
    /// </remarks>
    public void RevokeAllRefreshTokens()
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
        }
    }

    /// <summary>
    /// Actualiza la foto de perfil del usuario.
    /// </summary>
    /// <param name="photoUrl">Nueva URL de la foto de perfil.</param>
    public void UpdatePhoto(string photoUrl)
    {
        PhotoUrl = photoUrl;
        UpdatedAt = DateTime.UtcNow;
    }
}
