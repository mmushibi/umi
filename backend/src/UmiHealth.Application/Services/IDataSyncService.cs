using System.Threading.Tasks;
using UmiHealth.Api.Controllers;

namespace UmiHealth.Application.Services
{
    public interface IDataSyncService
    {
        Task<SyncStatusDto> GetSyncStatusAsync();
        Task TriggerSyncAsync(string syncType);
    }
}
