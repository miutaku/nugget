using Nugget.Core.Entities;
using Nugget.Core.Enums;

namespace Nugget.Core.Tests;

public class TodoTests
{
    [Fact]
    public void Todo_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var todo = new Todo();

        // Assert
        Assert.True(todo.NotifyImmediately);
        Assert.Equal([3, 1, 0], todo.ReminderDays);
        Assert.NotNull(todo.Assignments);
        Assert.Empty(todo.Assignments);
    }

    [Fact]
    public void Todo_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var title = "月末工数入力";
        var description = "1月分の工数を入力してください";
        var dueDate = new DateTime(2026, 2, 28, 17, 0, 0, DateTimeKind.Utc);
        var createdById = Guid.NewGuid();

        // Act
        var todo = new Todo
        {
            Id = id,
            Title = title,
            Description = description,
            DueDate = dueDate,
            CreatedById = createdById,
            TargetType = TargetType.All,
            NotifyImmediately = false,
            ReminderDays = [7, 3, 1, 0]
        };

        // Assert
        Assert.Equal(id, todo.Id);
        Assert.Equal(title, todo.Title);
        Assert.Equal(description, todo.Description);
        Assert.Equal(dueDate, todo.DueDate);
        Assert.Equal(createdById, todo.CreatedById);
        Assert.Equal(TargetType.All, todo.TargetType);
        Assert.False(todo.NotifyImmediately);
        Assert.Equal([7, 3, 1, 0], todo.ReminderDays);
    }

    [Theory]
    [InlineData(TargetType.All)]
    [InlineData(TargetType.Group)]
    [InlineData(TargetType.Individual)]
    public void Todo_ShouldSupportAllTargetTypes(TargetType targetType)
    {
        // Arrange & Act
        var todo = new Todo { TargetType = targetType };

        // Assert
        Assert.Equal(targetType, todo.TargetType);
    }
}
