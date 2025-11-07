using System;
using System.Threading.Tasks;
using CCD.Core.Dtos;
using CCD.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CCD.Infrastructure.Services
{
    public class SendGridEmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _dashboardUrl;
        private readonly ILogger<SendGridEmailService> _logger;
        private readonly bool _isConfigured;

        public SendGridEmailService(IConfiguration config, ILogger<SendGridEmailService> logger)
        {
            _apiKey = config["SendGrid:ApiKey"];
            _fromEmail = config["SendGrid:FromEmail"] ?? "no-reply@apexdb.com";
            _fromName = config["SendGrid:FromName"] ?? "ApexDb Team";
            _dashboardUrl = config["App:DashboardUrl"] ?? "https://app.apexdb.com/dashboard";
            _logger = logger;

            if (string.IsNullOrEmpty(_apiKey))
            {
                _isConfigured = false;
                _logger.LogWarning("⚠️ Servicio de correo DESHABILITADO. Falta 'SendGrid:ApiKey'. Los correos no se enviarán.");
            }
            else
            {
                _isConfigured = true;
                _logger.LogInformation("✅ Servicio de correo inicializado correctamente");
                _logger.LogInformation($"📧 Remitente: {_fromName} <{_fromEmail}>");
            }
        }

        public async Task SendDatabaseCredentialsAsync(string toEmail, string userName, DatabaseConnectionDetails dbDetails)
        {
            try
            {
                if (!_isConfigured)
                {
                    _logger.LogWarning("📪 Envío de credenciales omitido: servicio de correo no configurado. Destinatario: {Email}", toEmail);
                    return;
                }

                _logger.LogInformation($"📨 Preparando envío de credenciales a: {toEmail}");

                var client = new SendGridClient(_apiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail, userName);
                var subject = "Tus credenciales de base de datos";
                
                var plainTextContent = $"""
Hola {userName},

Aquí están los detalles de tu nueva base de datos {dbDetails.Engine}:

Host: {dbDetails.Host}
Puerto: {dbDetails.Port}
Base de datos: {dbDetails.DatabaseName}
Usuario: {dbDetails.Username}
Contraseña: {dbDetails.Password}

Recuerda mantener esta información en un lugar seguro.
Puedes administrar tu instancia desde: {_dashboardUrl}

Gracias por usar nuestro servicio.
Equipo ApexDb
""";

                var htmlContent = $"""
<div style="font-family: 'Segoe UI', Arial, sans-serif; background-color: #0f0f0f; padding: 32px; color: #f5f5f5;">
  <table role="presentation" width="100%" style="max-width: 640px; margin: 0 auto; background-color: #121212; border: 1px solid #2a2a2a; border-radius: 18px; overflow: hidden;">
    <tr>
      <td style="background: linear-gradient(135deg, #1a1a1a 0%, #2f2f2f 70%, #d4af37 100%); padding: 36px 42px; color: #fef3c7;">
        <h1 style="margin: 0; font-size: 28px; font-weight: 700; letter-spacing: 0.6px;">Tu base de datos está lista ✨</h1>
        <p style="margin: 14px 0 0; font-size: 16px; color: #fefce8;">Hola {userName}, ya puedes conectarte a tu instancia {dbDetails.Engine}. Guarda estas credenciales en un lugar seguro.</p>
      </td>
    </tr>
    <tr>
      <td style="padding: 32px 42px;">
        <div style="border: 1px solid rgba(212, 175, 55, 0.45); border-radius: 14px; padding: 26px; background: rgba(19, 19, 19, 0.85);">
          <p style="margin: 0 0 16px; font-size: 12px; letter-spacing: 1.5px; text-transform: uppercase; color: #d4af37; font-weight: 600;">Credenciales de conexión</p>
          <table role="presentation" width="100%" style="border-collapse: collapse;">
            <tr>
              <td style="padding: 10px 0; font-size: 14px; color: #d1d5db; width: 40%;">Host</td>
              <td style="padding: 10px 0; font-size: 14px; font-weight: 600; color: #f9fafb;">{dbDetails.Host}</td>
            </tr>
            <tr>
              <td style="padding: 10px 0; font-size: 14px; color: #d1d5db;">Puerto</td>
              <td style="padding: 10px 0; font-size: 14px; font-weight: 600; color: #f9fafb;">{dbDetails.Port}</td>
            </tr>
            <tr>
              <td style="padding: 10px 0; font-size: 14px; color: #d1d5db;">Base de datos</td>
              <td style="padding: 10px 0; font-size: 14px; font-weight: 600; color: #f9fafb;">{dbDetails.DatabaseName}</td>
            </tr>
            <tr>
              <td style="padding: 10px 0; font-size: 14px; color: #d1d5db;">Usuario</td>
              <td style="padding: 10px 0; font-size: 14px; font-weight: 600; color: #f9fafb;">{dbDetails.Username}</td>
            </tr>
            <tr>
              <td style="padding: 10px 0; font-size: 14px; color: #d1d5db;">Contraseña</td>
              <td style="padding: 10px 0; font-size: 14px; font-weight: 600; color: #f9fafb;">{dbDetails.Password}</td>
            </tr>
          </table>
        </div>
        <div style="margin-top: 26px; padding: 20px 24px; border-radius: 12px; background: rgba(212, 175, 55, 0.12); border: 1px solid rgba(212, 175, 55, 0.35); color: #fef9c3;">
          <p style="margin: 0; font-size: 14px;"><strong style="color: #d4af37;">🔐 Consejo de seguridad:</strong> Actualiza tu contraseña periódicamente y nunca compartas estas credenciales por correo.</p>
        </div>
        <div style="margin-top: 34px; text-align: center;">
          <a href="{_dashboardUrl}" style="display: inline-block; padding: 15px 36px; background: linear-gradient(135deg, #d4af37, #8c6a15); color: #0f0f0f; text-decoration: none; border-radius: 999px; font-weight: 700; letter-spacing: 0.6px;">Ir a mi panel</a>
        </div>
      </td>
    </tr>
    <tr>
      <td style="padding: 0 42px 38px; color: #d1d5db; font-size: 13px; line-height: 1.6;">
        <p style="margin: 0 0 12px;">¿Necesitas ayuda? Escríbenos, estamos disponibles para ayudarte a conectar tu base de datos cuando lo necesites.</p>
        <p style="margin: 0; font-weight: 600; color: #d4af37;">Equipo ApexDb</p>
      </td>
    </tr>
  </table>
</div>
""";

                _logger.LogInformation("✉️ Creando mensaje de correo...");
                var msg = MailHelper.CreateSingleEmail(
                    from: from,
                    to: to,
                    subject: subject,
                    plainTextContent: plainTextContent,
                    htmlContent: htmlContent
                );
                
                _logger.LogInformation("🚀 Enviando correo a través de SendGrid...");
                var response = await client.SendEmailAsync(msg);
                
                _logger.LogInformation($"📩 Estado de la respuesta: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError($"❌ Error al enviar correo. Código: {response.StatusCode}. Respuesta: {responseBody}");
                    throw new Exception($"Error al enviar correo: {response.StatusCode} - {responseBody}");
                }

                _logger.LogInformation("✅ Correo con credenciales enviado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error crítico al enviar correo con credenciales");
                throw new Exception("No se pudo enviar el correo con las credenciales. Por favor, contacta al soporte técnico.", ex);
            }
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string userName)
        {
            try
            {
                if (!_isConfigured)
                {
                    _logger.LogWarning("📪 Envío de bienvenida omitido: servicio de correo no configurado. Destinatario: {Email}", toEmail);
                    return;
                }

                _logger.LogInformation($"📨 Preparando envío de bienvenida a: {toEmail}");

                var client = new SendGridClient(_apiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail, userName);
                var subject = "¡Bienvenido a CCD Platform!";
                
                var plainTextContent = $"""
Hola {userName},

¡Bienvenido a ApexDb! Tu cuenta ya está lista para crear y gestionar bases de datos.

Desde tu panel podrás:
- Crear instancias en segundos
- Administrar credenciales
- Monitorear el estado de tus proyectos

Ingresa aquí: {_dashboardUrl}

Si necesitas ayuda, estamos disponibles.
Equipo ApexDb
""";

                var htmlContent = $"""
<div style="font-family: 'Segoe UI', Arial, sans-serif; background-color: #0f0f0f; padding: 32px; color: #f5f5f5;">
  <table role="presentation" width="100%" style="max-width: 640px; margin: 0 auto; background-color: #121212; border: 1px solid #242424; border-radius: 18px; overflow: hidden;">
    <tr>
      <td style="background: linear-gradient(135deg, #1b1b1b 0%, #2b2b2b 65%, #d4af37 100%); padding: 36px 42px; color: #fef9c3;">
        <h1 style="margin: 0; font-size: 28px; font-weight: 700; letter-spacing: 0.5px;">¡Bienvenido a ApexDb, {userName}! 🏆</h1>
        <p style="margin: 14px 0 0; font-size: 16px; color: #fefce8;">Tu cuenta está lista para desplegar bases de datos potentes y seguras en cuestión de segundos.</p>
      </td>
    </tr>
    <tr>
      <td style="padding: 32px 42px;">
        <p style="margin: 0 0 20px; font-size: 16px; line-height: 1.7; color: #e5e7eb;">Comienza explorando todas las herramientas disponibles en tu panel:</p>
        <ul style="margin: 0 0 26px 18px; padding: 0; color: #f9fafb; font-size: 15px; line-height: 1.75;">
          <li style="margin-bottom: 12px;"><strong style="color: #d4af37;">Crea bases de datos</strong> en segundos con los motores más populares.</li>
          <li style="margin-bottom: 12px;"><strong style="color: #d4af37;">Gestiona credenciales</strong> y accesos desde un solo lugar.</li>
          <li style="margin-bottom: 12px;"><strong style="color: #d4af37;">Monitorea tus instancias</strong> con métricas en tiempo real.</li>
        </ul>
        <div style="margin: 30px 0; text-align: center;">
          <a href="{_dashboardUrl}" style="display: inline-block; padding: 15px 40px; background: linear-gradient(135deg, #d4af37, #8c6a15); color: #0f0f0f; text-decoration: none; border-radius: 999px; font-weight: 700; letter-spacing: 0.6px;">Ir al panel</a>
        </div>
        <div style="padding: 20px 24px; border-radius: 12px; background: rgba(212, 175, 55, 0.12); border: 1px solid rgba(212, 175, 55, 0.35); color: #fef9c3;">
          <p style="margin: 0; font-size: 14px;"><strong style="color: #d4af37;">¿Necesitas ayuda?</strong> Nuestro equipo está disponible para acompañarte en la puesta en marcha de tus proyectos.</p>
        </div>
      </td>
    </tr>
    <tr>
      <td style="padding: 0 42px 38px; color: #d1d5db; font-size: 13px; line-height: 1.6;">
        <p style="margin: 0;">Gracias por confiar en nosotros.<br /><strong style="color: #d4af37;">Equipo ApexDb</strong></p>
      </td>
    </tr>
  </table>
</div>
""";

                _logger.LogInformation("✉️ Creando mensaje de bienvenida...");
                var msg = MailHelper.CreateSingleEmail(
                    from: from,
                    to: to,
                    subject: subject,
                    plainTextContent: plainTextContent,
                    htmlContent: htmlContent
                );
                
                _logger.LogInformation("🚀 Enviando correo de bienvenida...");
                var response = await client.SendEmailAsync(msg);
                
                _logger.LogInformation($"📩 Estado de la respuesta: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError($"❌ Error al enviar correo de bienvenida. Código: {response.StatusCode}. Respuesta: {responseBody}");
                    // No lanzamos excepción para no afectar el flujo principal
                    return;
                }

                _logger.LogInformation("✅ Correo de bienvenida enviado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "⚠️ Error al enviar correo de bienvenida");
                // No lanzamos la excepción para no afectar el flujo principal
            }
        }
    }
}
