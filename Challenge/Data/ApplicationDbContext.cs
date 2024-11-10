using Microsoft.EntityFrameworkCore;
using Challenge.Models;

namespace Challenge.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Show> Shows { get; set; }
        public DbSet<Network> Networks { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Externals> Externals { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Rating> Ratings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            // Country
            modelBuilder.Entity<Country>()
                .HasKey(c => c.Code);
            modelBuilder.Entity<Country>()
                .Property(c => c.Code)
                .ValueGeneratedNever();

            // Network
            modelBuilder.Entity<Network>()
                .HasKey(n => n.Id);
            modelBuilder.Entity<Network>()
                .Property(n => n.Id)
                .ValueGeneratedNever();
            modelBuilder.Entity<Network>()
                .HasOne(n => n.Country)
                .WithMany()
                .HasForeignKey(n => n.CountryCode)
                .OnDelete(DeleteBehavior.Restrict);

            // Network -> Shows (one-to-many)
            modelBuilder.Entity<Network>()
                .HasMany(n => n.Shows)
                .WithOne(s => s.Network)
                .HasForeignKey(s => s.NetworkId)
                .OnDelete(DeleteBehavior.Restrict);

            // Show
            modelBuilder.Entity<Show>()
                .HasKey(s => s.Id);
            modelBuilder.Entity<Show>()
                .Property(s => s.Id)
                .ValueGeneratedNever();

            // Externals
            modelBuilder.Entity<Externals>()
                .HasKey(e => e.Id);
            modelBuilder.Entity<Externals>()
                .Property(e => e.Id)
                .ValueGeneratedNever();
            modelBuilder.Entity<Externals>()
                .Property(e => e.Imdb)
                .IsRequired(false); // Allow nulls in Imdb
            modelBuilder.Entity<Externals>()
                .HasOne(e => e.Show)
                .WithOne(s => s.Externals)
                .HasForeignKey<Externals>(e => e.Id);

            // Rating
            modelBuilder.Entity<Rating>()
                .HasKey(r => r.Id);
            modelBuilder.Entity<Rating>()
                .Property(r => r.Id)
                .ValueGeneratedNever();
            modelBuilder.Entity<Rating>()
                .HasOne(r => r.Show)
                .WithOne(s => s.Rating)
                .HasForeignKey<Rating>(r => r.Id);

            // Genre
            modelBuilder.Entity<Genre>()
                .HasKey(g => g.Id);
            modelBuilder.Entity<Genre>()
                .Property(g => g.Id)
                .ValueGeneratedOnAdd();

            // Show -> Genre (many-to-many)
            modelBuilder.Entity<Show>()
                .HasMany(s => s.Genres)
                .WithMany(g => g.Shows)
                .UsingEntity(j => j.ToTable("ShowGenres"));
        }
    }
}
