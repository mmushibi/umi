using System.Threading;
using System.Threading.Tasks;

namespace UmiHealth.Core.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
        Task<bool> SendEmailWithTemplateAsync(string to, string templateName, object templateData, CancellationToken cancellationToken = default);
        Task<bool> SendBulkEmailAsync(string[] recipients, string subject, string body, CancellationToken cancellationToken = default);
    }
}
