using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using WorkScholarship.Application.Common.Email;
using WorkScholarship.Application.Common.Interfaces;
using WorkScholarship.Application.Common.Models;
using WorkScholarship.Domain.Interfaces;

namespace WorkScholarship.Application.Features.Auth.Commands.ForgotPassword;

/// <summary>
/// Handler que procesa la solicitud de recuperación de contraseña.
/// </summary>
/// <remarks>
/// Flujo:
/// 1. Busca el usuario por email
/// 2. Si no existe o está inactivo, retorna éxito igualmente (no revelar existencia de emails)
/// 3. Genera un token criptográfico de un solo uso con expiración de 1 hora
/// 4. Persiste el token en la entidad User
/// 5. Construye la URL de reset con el token como query param
/// 6. Envía el email de recuperación mediante IEmailService
/// 7. Retorna éxito siempre (prevención de enumeración de usuarios)
/// </remarks>
public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly EmailSettings _emailSettings;

    /// <summary>
    /// Inicializa el handler con las dependencias necesarias.
    /// </summary>
    /// <param name="context">Contexto de base de datos para buscar el usuario.</param>
    /// <param name="emailService">Servicio para enviar el email con el enlace de reset.</param>
    /// <param name="dateTimeProvider">Proveedor de fecha/hora para calcular la expiración del token.</param>
    /// <param name="emailSettings">Configuración de email: FrontendUrl para construir el enlace de reset.</param>
    public ForgotPasswordCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        IDateTimeProvider dateTimeProvider,
        IOptions<EmailSettings> emailSettings)
    {
        _context = context;
        _emailService = emailService;
        _dateTimeProvider = dateTimeProvider;
        _emailSettings = emailSettings.Value;
    }

    /// <summary>
    /// Procesa la solicitud de recuperación de contraseña.
    /// </summary>
    /// <param name="request">Comando con el email del usuario que solicita el reset.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación.</param>
    /// <returns>
    /// Siempre retorna Result.Success() independientemente de si el email existe,
    /// para prevenir la enumeración de usuarios registrados en el sistema.
    /// </returns>
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        // Siempre responder éxito: no revelar si el email está registrado
        if (user is null || !user.IsActive)
        {
            return Result.Success();
        }

        var resetToken = GenerateSecureToken();
        var expiresAt = _dateTimeProvider.UtcNow.AddHours(1);

        user.SetPasswordResetToken(resetToken, expiresAt);

        await _context.SaveChangesAsync(cancellationToken);

        var resetUrl = $"{_emailSettings.FrontendUrl}/auth/reset-password?token={resetToken}";

        var emailMessage = PasswordEmailTemplates.PasswordReset(
            recipientName: user.FullName,
            recipientEmail: user.Email,
            resetUrl: resetUrl);

        await _emailService.SendAsync(emailMessage, cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Genera un token criptográficamente seguro de 64 bytes representado en formato hexadecimal.
    /// Produce una cadena de 128 caracteres en minúsculas adecuada para uso como token de un solo uso.
    /// </summary>
    /// <returns>Cadena hexadecimal de 128 caracteres.</returns>
    private static string GenerateSecureToken()
    {
        var tokenBytes = new byte[64];
        RandomNumberGenerator.Fill(tokenBytes);
        return Convert.ToHexString(tokenBytes).ToLowerInvariant();
    }
}
