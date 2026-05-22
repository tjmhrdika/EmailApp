using EmailApp.Models;

namespace EmailApp.Services
{
    public interface IEmailRecipientService
    {
        Task<IReadOnlyList<Email>> GetEmailsAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EmailGroup>> GetGroupsAsync(CancellationToken cancellationToken = default);
        Task<(bool Success, string? ErrorMessage)> AddEmailAsync(Email email, CancellationToken cancellationToken = default);
        Task<(bool Success, string? ErrorMessage)> UpdateEmailAsync(Email email, CancellationToken cancellationToken = default);
        Task DeleteEmailAsync(Guid emailId, CancellationToken cancellationToken = default);
        Task<(bool Success, string? ErrorMessage)> AddGroupAsync(EmailGroup group, CancellationToken cancellationToken = default);
        Task<(bool Success, string? ErrorMessage)> UpdateGroupAsync(EmailGroup group, CancellationToken cancellationToken = default);
        Task DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
    }
}
