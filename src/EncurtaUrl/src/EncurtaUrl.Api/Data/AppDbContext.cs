using EncurtaUrl.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EncurtaUrl.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ShortUrl> ShortUrls => Set<ShortUrl>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortUrl>(entity =>
        {
            entity.ToTable("ShortUrls");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(32)
                .UseCollation("NOCASE");

            entity.HasIndex(x => x.Code)
                .IsUnique();

            entity.Property(x => x.OriginalUrl)
                .IsRequired()
                .HasMaxLength(2048);
        });
    }
}
