using Microsoft.EntityFrameworkCore;
using HealthAppApi.Models;

namespace HealthAppApi.Data
{
    public class HealthAppContext : DbContext
    {
        public HealthAppContext(DbContextOptions<HealthAppContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Record> Records { get; set; }
        public DbSet<BillboardRecord> BillboardRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Record>().ToTable("activity_records");
            modelBuilder.Entity<BillboardRecord>().ToTable("billboard_records");
        }
    }
}