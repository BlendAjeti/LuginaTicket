using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LuginaTicket.Models;

namespace LuginaTicket.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Movie> Movies { get; set; }
    public DbSet<Showtime> Showtimes { get; set; }
    public DbSet<CinemaHall> CinemaHalls { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<ActionLog> ActionLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Movie configuration
        builder.Entity<Movie>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Title);
        });

        // Showtime configuration
        builder.Entity<Showtime>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Movie)
                .WithMany(m => m.Showtimes)
                .HasForeignKey(e => e.MovieId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CinemaHall)
                .WithMany(h => h.Showtimes)
                .HasForeignKey(e => e.CinemaHallId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Seat configuration
        builder.Entity<Seat>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Showtime)
                .WithMany(s => s.Seats)
                .HasForeignKey(e => e.ShowtimeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CinemaHall)
                .WithMany(h => h.Seats)
                .HasForeignKey(e => e.CinemaHallId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Ticket)
                .WithOne(t => t.Seat)
                .HasForeignKey<Ticket>(t => t.SeatId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Ticket configuration
        builder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TicketNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.TicketNumber).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany(u => u.Tickets)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Showtime)
                .WithMany(s => s.Tickets)
                .HasForeignKey(e => e.ShowtimeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ActionLog configuration
        builder.Entity<ActionLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
        });
    }
}

