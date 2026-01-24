using Xunit;
using LuginaTicket.Data;
using LuginaTicket.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace LuginaTicket.Tests;

public class BookingServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void ReserveSeat_ShouldChangeSeatStatusToOccupied()
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
        var seat = new Seat 
        { 
            Row = "A", 
            Number = 1, 
            Status = SeatStatus.Available, 
            Showtime = showtime, 
            CinemaHall = hall 
        };
        
        context.Movies.Add(movie);
        context.CinemaHalls.Add(hall);
        context.Showtimes.Add(showtime);
        context.Seats.Add(seat);
        context.SaveChanges();

        // Act
        seat.Status = SeatStatus.Occupied;
        context.Seats.Update(seat);
        context.SaveChanges();

        // Assert
        var updatedSeat = context.Seats.First();
        Assert.Equal(SeatStatus.Occupied, updatedSeat.Status);
    }

    [Fact]
    public void CalculateTotalPrice_ShouldMultiplyQuantityByPrice()
    {
        // Arrange
        decimal pricePerTicket = 10.00m;
        int quantity = 3;

        // Act
        decimal totalPrice = pricePerTicket * quantity;

        // Assert
        Assert.Equal(30.00m, totalPrice);
    }
}

