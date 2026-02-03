using Microsoft.EntityFrameworkCore;
using Nugget.Core.Entities;
using Nugget.Core.Enums;
using Nugget.Infrastructure.Data;
using Nugget.Infrastructure.Repositories;

namespace Nugget.Infrastructure.Tests;

public class UserRepositoryTests
{
    private static NuggetDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<NuggetDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new NuggetDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ShouldAddUserToDatabase()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            Role = UserRole.User
        };

        // Act
        var result = await repository.AddAsync(user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Email, result.Email);
        
        var savedUser = await context.Users.FindAsync(user.Id);
        Assert.NotNull(savedUser);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUserWithNotificationSetting()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);
        
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Name = "Test User"
        };
        context.Users.Add(user);
        
        var setting = new NotificationSetting
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DaysBeforeDue = [5, 2, 0]
        };
        context.NotificationSettings.Add(setting);
        
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.NotificationSetting);
        Assert.Equal([5, 2, 0], result.NotificationSetting.DaysBeforeDue);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnCorrectUser()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);
        
        var email = "unique@example.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = "Unique User"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByEmailAsync(email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNullForNonExistentEmail()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetByEmailAsync("nonexistent@example.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBySamlNameIdAsync_ShouldReturnCorrectUser()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);
        
        var samlNameId = "saml123";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "saml@example.com",
            Name = "SAML User",
            SamlNameId = samlNameId
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetBySamlNameIdAsync(samlNameId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(samlNameId, result.SamlNameId);
    }

    [Fact]
    public async Task GetAllActiveUsersAsync_ShouldReturnOnlyActiveUsers()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);
        
        var activeUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "active@example.com",
            Name = "Active User",
            IsActive = true
        };
        
        var inactiveUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "inactive@example.com",
            Name = "Inactive User",
            IsActive = false
        };
        
        context.Users.AddRange(activeUser, inactiveUser);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllActiveUsersAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("active@example.com", result[0].Email);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUser()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "update@example.com",
            Name = "Original Name"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        user.Name = "Updated Name";
        user.SlackUserId = "U123456";
        await repository.UpdateAsync(user);

        // Assert
        var updatedUser = await context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal("Updated Name", updatedUser.Name);
        Assert.Equal("U123456", updatedUser.SlackUserId);
    }
}
