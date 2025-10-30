using CCD.Core;
using System;
using System.Threading.Tasks;

namespace CCD.Core.Interfaces
{
    public interface IDatabaseProvisioner
    {
        // Devuelve los detalles de la instancia creada o nulo si falla.
        Task<DatabaseInstance?> CreateDatabaseAsync(User user, string engine);

        // Devuelve true si se borró exitosamente, false si no.
        Task<bool> DeleteDatabaseAsync(Guid instanceId, User user);

        // Nuevo método para obtener las bases de datos de un usuario
        Task<IEnumerable<DatabaseInstance>> GetUserDatabasesAsync(Guid userId);
    }
}