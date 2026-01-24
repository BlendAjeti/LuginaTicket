using Xunit;
using LuginaTicket.Data;
using LuginaTicket.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace LuginaTicket.Tests;

public class MovieServiceTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void CreateMovie_ShouldAddMovieToDatabase()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var movie = new Movie
        {
            Title = "Test Movie",
            Description = "Test Description",
            Genre = "Action",
            Duration = 120,
            ReleaseDate = DateTime.Now,
            Director = "Test Director",
            Actors = "Test Actor",
            IsActive = true
        };

        // Act
        context.Movies.Add(movie);
        context.SaveChanges();

        // Assert
        Assert.Single(context.Movies);
        Assert.Equal("Test Movie", context.Movies.First().Title);
    }

    [Fact]
    public void GetActiveMovies_ShouldReturnOnlyActiveMovies()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        context.Movies.AddRange(
            new Movie { Title = "Active Movie", IsActive = true },
            new Movie { Title = "Inactive Movie", IsActive = false }
        );
        context.SaveChanges();

        // Act
        var activeMovies = context.Movies.Where(m => m.IsActive).ToList();

        // Assert
        Assert.Single(activeMovies);
        Assert.Equal("Active Movie", activeMovies.First().Title);
    }

    [Fact]
    public void UpdateMovie_ShouldUpdateMovieProperties()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var movie = new Movie
        {
            Title = "Original Title",
            Description = "Original Description",
            IsActive = true
        };
        context.Movies.Add(movie);
        context.SaveChanges();

        // Act
        movie.Title = "Updated Title";
        movie.Description = "Updated Description";
        context.Movies.Update(movie);
        context.SaveChanges();

        // Assert
        var updatedMovie = context.Movies.First();
        Assert.Equal("Updated Title", updatedMovie.Title);
        Assert.Equal("Updated Description", updatedMovie.Description);
    }
}

