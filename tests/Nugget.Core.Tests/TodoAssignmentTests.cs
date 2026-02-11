using Nugget.Core.Entities;

namespace Nugget.Core.Tests;

public class TodoAssignmentTests
{
    [Fact]
    public void TodoAssignment_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var assignment = new TodoAssignment();

        // Assert
        Assert.False(assignment.IsCompleted);
        Assert.Null(assignment.CompletedAt);
        Assert.Null(assignment.LastNotifiedAt);
    }

    [Fact]
    public void TodoAssignment_ShouldTrackCompletion()
    {
        // Arrange
        var assignment = new TodoAssignment
        {
            Id = Guid.NewGuid(),
            TodoId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };

        // Act
        assignment.IsCompleted = true;
        assignment.CompletedAt = DateTime.UtcNow;

        // Assert
        Assert.True(assignment.IsCompleted);
        Assert.NotNull(assignment.CompletedAt);
    }

    [Fact]
    public void TodoAssignment_ShouldTrackNotification()
    {
        // Arrange
        var assignment = new TodoAssignment();
        var notifiedTime = DateTime.UtcNow;

        // Act
        assignment.LastNotifiedAt = notifiedTime;

        // Assert
        Assert.Equal(notifiedTime, assignment.LastNotifiedAt);
    }
}
