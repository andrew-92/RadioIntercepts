using Microsoft.EntityFrameworkCore;
using RadioIntercepts.Core.Models;

namespace RadioIntercepts.Infrastructure.Data
{
    public static class ModelIndexesConfiguration
    {
        public static void ConfigureIndexes(this ModelBuilder modelBuilder)
        {
            // Area: индекс по имени
            modelBuilder.Entity<Area>()
                .HasIndex(a => a.Name)
                .HasDatabaseName("IX_Areas_Name");

            // Frequency: индекс по значению
            modelBuilder.Entity<Frequency>()
                .HasIndex(f => f.Value)
                .HasDatabaseName("IX_Frequencies_Value");

            // Callsign: индекс по имени
            modelBuilder.Entity<Callsign>()
                .HasIndex(c => c.Name)
                .HasDatabaseName("IX_Callsigns_Name");

            // Message: индекс по дате
            modelBuilder.Entity<Message>()
                .HasIndex(m => m.DateTime)
                .HasDatabaseName("IX_Messages_DateTime");
        }
    }
}
