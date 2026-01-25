using LuginaTicket.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LuginaTicket.Data;

public static class SeedData
{
    public static async Task InitializeAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // Ensure database is created
        await context.Database.MigrateAsync();

        // Create roles
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole("User"));
        }

        // Create admin user
        var adminEmail = "admin@luginaticket.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Seed Cinema Halls
        if (!context.CinemaHalls.Any())
        {
            context.CinemaHalls.AddRange(
                new CinemaHall
                {
                    Name = "Fortesa",
                    Location = "CINEPLEXX PRISHTINE",
                    TotalRows = 11,
                    SeatsPerRow = 25,
                    IsActive = true
                },
                new CinemaHall
                {
                    Name = "Hall 2",
                    Location = "CINEPLEXX PRISHTINE",
                    TotalRows = 10,
                    SeatsPerRow = 20,
                    IsActive = true
                }
            );
            await context.SaveChangesAsync();
        }

        // Seed sample movies if none exist
        if (!context.Movies.Any())
        {
            var movies = new List<Movie>
            {
                new Movie
                {
                    Title = "ADN",
                    Description = "ADN është një dramë e fuqishme, me aksion të ngjeshur dhe nota romance të pamundura, ku trashëgimia e dhunës sfidohet nga forca e zgjedhjes.",
                    Genre = "Dramë, Aksion, Romantik",
                    Duration = 105,
                    ReleaseDate = new DateTime(2025, 11, 19),
                    Director = "Lindar Kaja",
                    Actors = "Romir Zalla, Suela Bako, Rike Roçi, Erjola Doçi, Julian Deda",
                    Distributor = "RKS Papadhimitri film production",
                    IsActive = true
                },
                new Movie
                {
                    Title = "BLACK PHONE 2",
                    Description = "A thrilling horror sequel",
                    Genre = "Horror, Thriller",
                    Duration = 120,
                    ReleaseDate = new DateTime(2025, 11, 19),
                    Director = "Scott Derrickson",
                    Actors = "Ethan Hawke",
                    Distributor = "Blumhouse",
                    IsActive = true
                }
            };

            context.Movies.AddRange(movies);
            await context.SaveChangesAsync();

            // Create showtimes for the movies
            var hall = context.CinemaHalls.First();
            var movie = movies.First();

            var showtime = new Showtime
            {
                MovieId = movie.Id,
                CinemaHallId = hall.Id,
                ShowDateTime = new DateTime(2025, 8, 15, 18, 0, 0),
                ViewType = "2D",
                Price = 3.20m,
                IsActive = true
            };

            context.Showtimes.Add(showtime);
            await context.SaveChangesAsync();

            // Create seats for the showtime
            var seats = new List<Seat>();
            var rows = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K" };
            
            for (int rowIndex = 0; rowIndex < Math.Min(hall.TotalRows, rows.Length); rowIndex++)
            {
                for (int seatNum = 1; seatNum <= hall.SeatsPerRow; seatNum++)
                {
                    seats.Add(new Seat
                    {
                        ShowtimeId = showtime.Id,
                        CinemaHallId = hall.Id,
                        Row = rows[rowIndex],
                        Number = seatNum,
                        Status = SeatStatus.Available,
                        IsWheelchairAccessible = rowIndex == 0 && seatNum == 1, // First seat in first row
                        IsVIP = false
                    });
                }
            }

            context.Seats.AddRange(seats);
            await context.SaveChangesAsync();
        }
    }
}

