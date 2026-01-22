// Infrastructure/Data/AlertDbContext.cs
using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Core.Models.Alerts;

namespace RadioIntercepts.Infrastructure.Data
{
    public class AlertDbContext : DbContext
    {
        public DbSet<AlertRule> AlertRules { get; set; }
        public DbSet<Alert> Alerts { get; set; }

        public AlertDbContext(DbContextOptions<AlertDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AlertRule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.ConditionExpression).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasIndex(e => e.IsEnabled);
                entity.HasIndex(e => e.LastChecked);
            });

            modelBuilder.Entity<Alert>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Details).HasColumnType("nvarchar(max)");
                entity.Property(e => e.DetectedAt).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.ResolutionNotes).HasMaxLength(2000);

                entity.HasOne(e => e.Rule)
                      .WithMany()
                      .HasForeignKey(e => e.RuleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Severity);
                entity.HasIndex(e => e.DetectedAt);
                entity.HasIndex(e => e.RuleId);
            });

            // Для хранения списков в JSON формате
            modelBuilder.Entity<Alert>()
                .Property(e => e.RelatedCallsigns)
                .HasConversion(
                    v => string.Join(";", v),
                    v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());

            modelBuilder.Entity<Alert>()
                .Property(e => e.RelatedAreas)
                .HasConversion(
                    v => string.Join(";", v),
                    v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());

            modelBuilder.Entity<Alert>()
                .Property(e => e.RelatedFrequencies)
                .HasConversion(
                    v => string.Join(";", v),
                    v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());

            modelBuilder.Entity<Alert>()
                .Property(e => e.RelatedMessageIds)
                .HasConversion(
                    v => string.Join(";", v.Select(id => id.ToString())),
                    v => v.Split(';', StringSplitOptions.RemoveEmptyEntries)
                          .Select(long.Parse)
                          .ToList());
        }
    }
}