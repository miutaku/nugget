using Nugget.Core.Entities;
using Nugget.Core.Enums;

namespace Nugget.Core.Tests;

public class UserTests
{
    [Fact]
    public void User_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        Assert.Equal(UserRole.User, user.Role);
        Assert.True(user.IsActive);
        Assert.NotNull(user.Assignments);
        Assert.Empty(user.Assignments);
        Assert.NotNull(user.CreatedTodos);
        Assert.Empty(user.CreatedTodos);
    }

    [Fact]
    public void User_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var email = "test@example.com";
        var name = "Test User";
        var slackUserId = "U123456";

        // Act
        var user = new User
        {
            Id = id,
            Email = email,
            Name = name,
            SlackUserId = slackUserId,
            Role = UserRole.Admin
        };

        // Assert
        Assert.Equal(id, user.Id);
        Assert.Equal(email, user.Email);
        Assert.Equal(name, user.Name);
        Assert.Equal(slackUserId, user.SlackUserId);
        Assert.Equal(UserRole.Admin, user.Role);
    }
}
