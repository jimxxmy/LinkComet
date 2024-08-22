using Microsoft.EntityFrameworkCore;

namespace UrlShortner.Data.Context
{
    public class UrlShortnerContext : DbContext
    {
        public UrlShortnerContext(DbContextOptions<UrlShortnerContext> options)
        : base(options)
        {
        }
        public UrlShortnerContext()
        {
            
        }
        public DbSet<ShortUrl> ShortenUrls { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=(LocalDb)\\MSSQLLocalDB;Database=UrlShortner;MultipleActiveResultSets=true");
            }
        }
    }
}
