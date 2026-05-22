using EmailApp.Data;
using EmailApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailApp.Services
{
    public class EmailRecipientService : IEmailRecipientService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public EmailRecipientService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<IReadOnlyList<Email>> GetEmailsAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

            return await db.Emails
                .AsNoTracking()
                .OrderBy(email => email.Address)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<EmailGroup>> GetGroupsAsync(CancellationToken cancellationToken = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

            return await db.EmailGroups
                .AsNoTracking()
                .OrderBy(group => group.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<(bool Success, string? ErrorMessage)> AddEmailAsync(Email email, CancellationToken cancellationToken = default)
        {
            var normalizedAddress = Normalize(email.Address);

            if (string.IsNullOrWhiteSpace(normalizedAddress))
                return (false, "Email address is required");

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var exists = await db.Emails.AnyAsync(existingEmail => existingEmail.Address == normalizedAddress, cancellationToken);

            if (exists)
                return (false, "Email already exists");

            email.Id = email.Id == Guid.Empty ? Guid.NewGuid() : email.Id;
            email.Address = normalizedAddress;

            db.Emails.Add(email);
            await db.SaveChangesAsync(cancellationToken);

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateEmailAsync(Email email, CancellationToken cancellationToken = default)
        {
            var normalizedAddress = Normalize(email.Address);

            if (string.IsNullOrWhiteSpace(normalizedAddress))
                return (false, "Email address is required");

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var duplicateExists = await db.Emails.AnyAsync(
                existingEmail => existingEmail.Id != email.Id && existingEmail.Address == normalizedAddress,
                cancellationToken);

            if (duplicateExists)
                return (false, "Email already exists");

            var currentEmail = await db.Emails.FirstOrDefaultAsync(existingEmail => existingEmail.Id == email.Id, cancellationToken);

            if (currentEmail == null)
                return (false, "Email not found");

            currentEmail.Address = normalizedAddress;
            currentEmail.EmailGroupId = email.EmailGroupId;

            await db.SaveChangesAsync(cancellationToken);

            return (true, null);
        }

        public async Task DeleteEmailAsync(Guid emailId, CancellationToken cancellationToken = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var email = await db.Emails.FirstOrDefaultAsync(existingEmail => existingEmail.Id == emailId, cancellationToken);

            if (email == null)
                return;

            db.Emails.Remove(email);
            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task<(bool Success, string? ErrorMessage)> AddGroupAsync(EmailGroup group, CancellationToken cancellationToken = default)
        {
            var normalizedName = Normalize(group.Name);

            if (string.IsNullOrWhiteSpace(normalizedName))
                return (false, "Group name is required");

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var exists = await db.EmailGroups.AnyAsync(existingGroup => existingGroup.Name == normalizedName, cancellationToken);

            if (exists)
                return (false, "Group already exists");

            group.Id = group.Id == Guid.Empty ? Guid.NewGuid() : group.Id;
            group.Name = normalizedName;

            db.EmailGroups.Add(group);
            await db.SaveChangesAsync(cancellationToken);

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateGroupAsync(EmailGroup group, CancellationToken cancellationToken = default)
        {
            var normalizedName = Normalize(group.Name);

            if (string.IsNullOrWhiteSpace(normalizedName))
                return (false, "Group name is required");

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var duplicateExists = await db.EmailGroups.AnyAsync(
                existingGroup => existingGroup.Id != group.Id && existingGroup.Name == normalizedName,
                cancellationToken);

            if (duplicateExists)
                return (false, "Group already exists");

            var currentGroup = await db.EmailGroups.FirstOrDefaultAsync(existingGroup => existingGroup.Id == group.Id, cancellationToken);

            if (currentGroup == null)
                return (false, "Group not found");

            currentGroup.Name = normalizedName;
            await db.SaveChangesAsync(cancellationToken);

            return (true, null);
        }

        public async Task DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var group = await db.EmailGroups.FirstOrDefaultAsync(existingGroup => existingGroup.Id == groupId, cancellationToken);

            if (group == null)
                return;

            await db.Emails
                .Where(email => email.EmailGroupId == groupId)
                .ExecuteUpdateAsync(setter => setter.SetProperty(email => email.EmailGroupId, (Guid?)null), cancellationToken);

            db.EmailGroups.Remove(group);
            await db.SaveChangesAsync(cancellationToken);
        }

        private static string Normalize(string value)
        {
            return value.Trim();
        }
    }
}
