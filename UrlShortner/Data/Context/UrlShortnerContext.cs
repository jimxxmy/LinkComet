using Microsoft.EntityFrameworkCore;

namespace UrlShortner.Data.Context
{
    public class UrlShortnerContext : DbContext
    {
        public const int ShortCodeMaxLength = 16;

        public UrlShortnerContext(DbContextOptions<UrlShortnerContext> options)
            : base(options)
        {
        }

        public DbSet<ShortUrl> ShortenUrls { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ShortUrl>(entity =>
            {
                entity.Property(su => su.ShortCode).HasMaxLength(ShortCodeMaxLength);
                entity.HasIndex(su => su.ShortCode).IsUnique();
            });
        }
    }
}
