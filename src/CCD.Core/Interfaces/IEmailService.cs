using System.Threading.Tasks;
using CCD.Core.Dtos;

namespace CCD.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendDatabaseCredentialsAsync(string toEmail, string userName, DatabaseConnectionDetails dbDetails);
        Task SendWelcomeEmailAsync(string toEmail, string userName);
    }
}
