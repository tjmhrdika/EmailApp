using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<User> Users { get; set; }
    public DbSet<Email> Emails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.HasIndex(u => u.Username)
                .IsUnique();
            entity.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(u => u.Password)
                .IsRequired()
                .HasMaxLength(500);
            entity.Property(u => u.IsAdmin)
                .IsRequired();
        });
        
        modelBuilder.Entity<Email>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.HasIndex(u => u.Address)
                .IsUnique();
        });
    }
}