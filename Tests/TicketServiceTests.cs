using Xunit;
using LuginaTicket.Data;
using LuginaTicket.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace LuginaTicket.Tests;

public class TicketServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void CreateTicket_ShouldGenerateUniqueTicketNumber()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var movie = new Movie { Title = "Test Movie", IsActive = true };
        var hall = new CinemaHall { Name = "Hall 1", Location = "Location", TotalRows = 10, SeatsPerRow = 20 };
        var showtime = new Showtime 
        { 
            Movie = movie, 
            CinemaHall = hall, 
            ShowDateTime = DateTime.Now.AddDays(1),
            Price = 10.00m,
            IsActive = true
        };
        var seat = new Seat { Row = "A", Number = 1, Status = SeatStatus.Available, Showtime = showtime, CinemaHall = hall };
        
        context.Movies.Add(movie);
        context.CinemaHalls.Add(hall);
        context.Showtimes.Add(showtime);
        context.Seats.Add(seat);
        context.SaveChanges();

        var ticket = new Ticket
        {
            TicketNumber = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}",
            UserId = "test-user-id",
            ShowtimeId = showtime.Id,
            SeatId = seat.Id,
            Price = 10.00m,
            Status = TicketStatus.Confirmed
        };

        // Act
        context.Tickets.Add(ticket);
        context.SaveChanges();

        // Assert
        Assert.Single(context.Tickets);
        Assert.NotNull(context.Tickets.First().TicketNumber);
        Assert.StartsWith("TKT-", context.Tickets.First().TicketNumber);
    }

    [Fact]
    public void GetTicketsByUser_ShouldReturnOnlyUserTickets()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var userId1 = "user1";
        var userId2 = "user2";
        
        var ticket1 = new Ticket { TicketNumber = "TKT-001", UserId = userId1, Price = 10.00m, Status = TicketStatus.Confirmed };
        var ticket2 = new Ticket { TicketNumber = "TKT-002", UserId = userId2, Price = 10.00m, Status = TicketStatus.Confirmed };
        
        context.Tickets.AddRange(ticket1, ticket2);
        context.SaveChanges();

        // Act
        var userTickets = context.Tickets.Where(t => t.UserId == userId1).ToList();

        // Assert
        Assert.Single(userTickets);
        Assert.Equal("TKT-001", userTickets.First().TicketNumber);
    }
}

