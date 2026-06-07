using EmailApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailApp.Data
{
    public class AlarmDbContext : DbContext
    {
        public AlarmDbContext(DbContextOptions<AlarmDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<AlarmDetail> AlarmDetails { get; set; }
        public DbSet<AlarmMaster> AlarmMasters { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AlarmDetail>(entity =>
            {
                entity.HasKey(e => e.AlarmDetailId);
                entity.ToTable("AlarmDetail");
                entity.Property(e => e.EventStamp)
                    .HasDefaultValueSql("GETDATE()")
                    .ValueGeneratedOnAddOrUpdate();
                entity.Property(e => e.Priority).HasColumnType("smallint");
                entity.HasOne(e => e.AlarmMaster)
                    .WithMany(e => e.AlarmDetails)
                    .HasForeignKey(e => e.AlarmId);
            });
            
            modelBuilder.Entity<AlarmMaster>(entity =>
            {
                entity.HasKey(e => e.AlarmId);
                entity.ToTable("AlarmMaster");
                entity.Property(e => e.Priority).HasColumnType("smallint");
            });
        }
    }
}