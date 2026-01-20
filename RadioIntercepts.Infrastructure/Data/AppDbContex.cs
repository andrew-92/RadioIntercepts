using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Core.Models;
using System.Reflection.Emit;

namespace RadioIntercepts.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Message> Messages => Set<Message>();
        public DbSet<Area> Areas => Set<Area>();
        public DbSet<Frequency> Frequencies => Set<Frequency>();
        public DbSet<Callsign> Callsigns => Set<Callsign>();
        public DbSet<MessageCallsign> MessageCallsigns => Set<MessageCallsign>();

        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<Area>()
                .HasIndex(a => a.Key)
                .IsUnique();

            model.Entity<Frequency>()
                .HasIndex(x => x.Value)
                .IsUnique();

            model.Entity<Callsign>()
                .HasIndex(x => x.Name)
                .IsUnique();

            model.Entity<Message>()
                .HasIndex(x => x.DateTime);

            model.Entity<Message>()
                .HasIndex(x => x.AreaId);

            model.Entity<Message>()
                .HasIndex(x => x.FrequencyId);

            model.Entity<MessageCallsign>()
                .HasKey(x => new { x.MessageId, x.CallsignId });

            model.ConfigureIndexes();
        }
    }
}
