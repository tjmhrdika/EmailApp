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

        public DbSet<AlarmDetail> AlarmDetails { get; set; }
        public DbSet<AlarmMaster> AlarmMasters { get; set; }
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
                entity.Property(e => e.EmailSent).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });
            
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "admin",
                    Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    IsAdmin = true
                }
            );

            modelBuilder.Entity<SetSmtp>().HasData(
                new SetSmtp
                {
                    Id = Guid.NewGuid(),
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
