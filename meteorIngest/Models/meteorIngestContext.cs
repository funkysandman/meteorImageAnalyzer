using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MeteorIngestAPI.Models
{
    public class MeteorIngestContext : DbContext
    {
        public MeteorIngestContext(DbContextOptions<MeteorIngestContext> options)
            : base(options)
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=skyImages.db");
        public DbSet<SkyImage> SkyImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SkyImage>()
                .Property(p => p.skyImageId)
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<SkyObjectDetection>()
                .Property(p => p.skyObjectID)
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<BoundingBox>()
                .Property(p => p.boundingBoxId)
                .ValueGeneratedOnAdd();
        }
    }
}
