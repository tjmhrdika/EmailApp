namespace EmailApp.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
        Task SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string body);
    }
}
