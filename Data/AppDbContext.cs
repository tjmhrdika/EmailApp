using Microsoft.EntityFrameworkCore;
using EmailApp.Models;

namespace EmailApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<Email> Emails { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<AlarmEmailTracking> AlarmEmailTracking { get; set; }
        public DbSet<EmailGroup> EmailGroups { get; set; }
        public DbSet<SetSmtp> SetSmtp { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<UserGroup>()
                .HasKey(ug => new { ug.UserId, ug.GroupId });
            
            modelBuilder.Entity<UserGroup>()
                .HasOne(ug => ug.User)
                .WithMany(u => u.UserGroups)
                .HasForeignKey(ug => ug.UserId);
                
            modelBuilder.Entity<UserGroup>()
                .HasOne(ug => ug.Group)
                .WithMany(g => g.UserGroups)
                .HasForeignKey(ug => ug.GroupId);
            
            modelBuilder.Entity<Group>()
                .HasIndex(g => g.Name)
                .IsUnique();
            
            modelBuilder.Entity<AlarmEmailTracking>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("AlarmEmailTracking");
                entity.HasIndex(e => e.AlarmDetailId).IsUnique();
                entity.Property(e => e.EmailSent).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<EmailGroup>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<Email>()
                .HasOne(e => e.EmailGroup)
                .WithMany(g => g.Emails)
                .HasForeignKey(e => e.EmailGroupId)
                .OnDelete(DeleteBehavior.SetNull);
            
            modelBuilder.Entity<SetSmtp>().HasData(
                new SetSmtp
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Host = "",
                    Port = 0,
                    User = "",
                    Pass = "",
                    FromEmail = ""
                }
            );
        }
    }
}
