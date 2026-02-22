namespace WorkScholarship.Application.Common.Email;

/// <summary>
/// Utilidad para construir emails a partir de templates HTML con placeholders.
/// </summary>
/// <remarks>
/// Los placeholders tienen el formato {{NombreClave}}.
/// El template base define solo el banner (cabecera) y el footer;
/// el contenido interno de cada email es responsabilidad de quien llama.
/// </remarks>
public static class EmailTemplateBuilder
{
    /// <summary>
    /// Layout base que envuelve cualquier contenido con un banner corporativo y un footer estándar.
    /// El placeholder {{Content}} se reemplaza con el cuerpo específico de cada email.
    /// </summary>
    private const string BASE_LAYOUT = """
        <!DOCTYPE html>
        <html lang="es">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>{{Subject}}</title>
        </head>
        <body style="margin:0;padding:0;background-color:#f4f6f9;font-family:Arial,Helvetica,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f6f9;padding:32px 0;">
            <tr>
              <td align="center">
                <table width="600" cellpadding="0" cellspacing="0" style="background-color:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);">
                  <!-- Banner -->
                  <tr>
                    <td style="background-color:#1e40af;padding:28px 40px;">
                      <p style="margin:0;color:#ffffff;font-size:20px;font-weight:bold;letter-spacing:0.5px;">
                        Work Scholarship System
                      </p>
                      <p style="margin:4px 0 0;color:#93c5fd;font-size:13px;">
                        Sistema de Gestión de Becas Trabajo
                      </p>
                    </td>
                  </tr>
                  <!-- Content -->
                  <tr>
                    <td style="padding:36px 40px;">
                      {{Content}}
                    </td>
                  </tr>
                  <!-- Footer -->
                  <tr>
                    <td style="background-color:#f8fafc;padding:20px 40px;border-top:1px solid #e2e8f0;">
                      <p style="margin:0;color:#94a3b8;font-size:12px;line-height:1.6;">
                        Este es un mensaje automático del sistema. Por favor no respondas a este correo.
                        Si no solicitaste esta acción, puedes ignorar este mensaje con seguridad.
                      </p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    /// <summary>
    /// Reemplaza los placeholders {{Key}} de un template con los valores del diccionario.
    /// </summary>
    /// <param name="template">Cadena de texto con placeholders {{Key}}.</param>
    /// <param name="values">Pares clave-valor para sustituir en el template.</param>
    /// <returns>Cadena resultante con todos los placeholders reemplazados.</returns>
    public static string ApplyPlaceholders(string template, Dictionary<string, string> values)
    {
        foreach (var (key, value) in values)
        {
            template = template.Replace($"{{{{{key}}}}}", value);
        }

        return template;
    }

    /// <summary>
    /// Envuelve un bloque de contenido HTML dentro del layout base (banner + footer).
    /// </summary>
    /// <param name="subject">Asunto del email, usado en el &lt;title&gt; del HTML.</param>
    /// <param name="contentHtml">HTML del cuerpo específico del email (ya con placeholders resueltos).</param>
    /// <returns>HTML completo del email listo para enviar.</returns>
    public static string WrapInLayout(string subject, string contentHtml)
    {
        return ApplyPlaceholders(BASE_LAYOUT, new Dictionary<string, string>
        {
            { "Subject", subject },
            { "Content", contentHtml }
        });
    }
}
