using System.Threading.Tasks;
using CCD.Core.Dtos;

namespace CCD.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendDatabaseCredentialsAsync(string toEmail, string userName, DatabaseConnectionDetails dbDetails);
        Task SendWelcomeEmailAsync(string toEmail, string userName);
        Task SendDatabaseDeletionEmailAsync(string toEmail, string userName, string databaseName, string engine);
        Task SendPlanChangeEmailAsync(string toEmail, string userName, string oldPlanName, string newPlanName, decimal newPrice);
        Task SendEmailVerificationAsync(string toEmail, string userName, string verificationToken);
        Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken);
    }
}
