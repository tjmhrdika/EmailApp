namespace EmailApp.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
        Task SendBulkEmailAsync(List<string> recipients, string subject, string body);
    }
}