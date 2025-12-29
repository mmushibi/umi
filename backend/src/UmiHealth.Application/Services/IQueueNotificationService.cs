using System.Threading.Tasks;
using UmiHealth.Application.DTOs.Queue;

namespace UmiHealth.Application.Services
{
    public interface IQueueNotificationService
    {
        Task SendQueueNotificationAsync(QueueNotificationRequest request);
    }
}
