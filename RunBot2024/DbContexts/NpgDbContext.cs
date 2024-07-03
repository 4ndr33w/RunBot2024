using Microsoft.EntityFrameworkCore;
using RunBot2024.Models;

namespace RunBot2024.DbContexts
{
    public class NpgDbContext : DbContext
    {
        private readonly DbContextOptions _options;
        private readonly IConfiguration _configuration;

        public NpgDbContext (DbContextOptions options, IConfiguration configuration)
        {
            _options = options;
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(_configuration.GetConnectionString("NpgConnection"),
                options => options.EnableRetryOnFailure(5, TimeSpan.FromSeconds(2), null));
        }

        public DbSet<RivalModel> Rivals { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<ErrorLog> Errors { get; set; }
        public DbSet<ReplyLog> ReplyLogs { get; set; }
        public DbSet<ResultLog> Results { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .UseCollation("utf8_general_ci")
                .HasDefaultSchema(_configuration["PostgreDefaultSchema"]);

            modelBuilder.Entity<RivalModel>(e =>
            {
                e.ToTable(_configuration["RivalTable"])
                .HasKey(e => e.Id);
            });

            modelBuilder.Entity<ErrorLog>(e =>
            {
                e.ToTable(_configuration["ErrorLogTable"]);
                e.HasKey(e => e.Id);
            });

            modelBuilder.Entity<ReplyLog>(e => 
            { 
                e.ToTable(_configuration["ReportLogTable"]);
                e.HasKey(e => e.Id);
            });

            modelBuilder.Entity<ResultLog>(e => 
            { 
                e.ToTable(_configuration["ResultLogTable"]);
                e.HasKey(e => e.Id);
            });

            modelBuilder.Entity<City>(e =>
            {
                e.ToTable(_configuration["CityListTable"]);
                e.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Region>(e =>
            {
                e.ToTable(_configuration["RegionListTable"]);
                e.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Company>(e =>
            {
                e.ToTable(_configuration["CompanyListTable"]);
                e.HasKey(e => e.Id);
            });
        }
    }
}
