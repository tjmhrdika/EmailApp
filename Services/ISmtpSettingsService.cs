using EmailApp.Models;

namespace EmailApp.Services
{
    public interface ISmtpSettingsService
    {
        Task<SetSmtp> GetSettingsAsync(CancellationToken cancellationToken = default);
        Task SaveSettingsAsync(SetSmtp settings, CancellationToken cancellationToken = default);
    }
}
