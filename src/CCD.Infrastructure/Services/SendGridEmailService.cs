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
        private readonly ILogger<SendGridEmailService> _logger;

        public SendGridEmailService(IConfiguration config, ILogger<SendGridEmailService> logger)
        {
            _apiKey = config["SendGrid:ApiKey"];
            _fromEmail = config["SendGrid:FromEmail"] ?? "no-reply@apexdb.com";
            _fromName = config["SendGrid:FromName"] ?? "ApexDb Team";
            _logger = logger;

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("ERROR CRÍTICO: No se encontró la API Key de SendGrid");
                throw new ArgumentNullException("SendGrid:ApiKey", "La API Key de SendGrid es requerida");
            }

            _logger.LogInformation("✅ Servicio de correo inicializado correctamente");
            _logger.LogInformation($"📧 Remitente: {_fromName} <{_fromEmail}>");
        }

        public async Task SendDatabaseCredentialsAsync(string toEmail, string userName, DatabaseConnectionDetails dbDetails)
        {
            try
            {
                _logger.LogInformation($"📨 Preparando envío de credenciales a: {toEmail}");

                var client = new SendGridClient(_apiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail, userName);
                var subject = "Tus credenciales de base de datos";
                
                var plainTextContent = $@"
                Hola {userName},

                Aquí están los detalles de tu nueva base de datos {dbDetails.Engine}:

                - Host: {dbDetails.Host}
                - Puerto: {dbDetails.Port}
                - Nombre de la base de datos: {dbDetails.DatabaseName}
                - Usuario: {dbDetails.Username}
                - Contraseña: {dbDetails.Password}

                ¡Gracias por usar nuestro servicio!
                
                Si necesitas ayuda, no dudes en contactarnos.
                
                Atentamente,
                El equipo de ApexDb";

                _logger.LogInformation("✉️ Creando mensaje de correo...");
                var msg = MailHelper.CreateSingleEmail(
                    from: from,
                    to: to,
                    subject: subject,
                    plainTextContent: plainTextContent,
                    htmlContent: null
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
                _logger.LogInformation($"📨 Preparando envío de bienvenida a: {toEmail}");

                var client = new SendGridClient(_apiKey);
                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail, userName);
                var subject = "¡Bienvenido a CCD Platform!";
                
                var plainTextContent = $@"
                Hola {userName},

                ¡Bienvenido a ApexDb! Tu cuenta ha sido creada exitosamente.

                Estamos encantados de tenerte con nosotros. Ahora puedes:
                - Crear nuevas bases de datos
                - Gestionar tus credenciales
                - Explorar todas las características de nuestra plataforma

                Si tienes alguna pregunta o necesitas ayuda, no dudes en contactarnos.
                
                ¡Gracias por unirte a nosotros!
                
                Atentamente,
                El equipo Apexdb";

                _logger.LogInformation("✉️ Creando mensaje de bienvenida...");
                var msg = MailHelper.CreateSingleEmail(
                    from: from,
                    to: to,
                    subject: subject,
                    plainTextContent: plainTextContent,
                    htmlContent: null
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
