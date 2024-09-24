
using Location.Models;

namespace Location.Interfaces.Services
{
    public interface IProviderCallerLogic
    {
        Task<string> CallProviderApiAsync(Provider provider, string ipAddress);
    }
}
