using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Nugget.Core.Entities;
using Nugget.Core.Enums;
using Nugget.Core.Interfaces;
using Nugget.Api.DTOs;
using Nugget.Api.Services;
using Nugget.Infrastructure.Data;

namespace Nugget.Api.Tests;

public class TodoServiceTests
{
    private readonly Mock<ITodoRepository> _todoRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IGroupRepository> _groupRepositoryMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly Mock<ILogger<TodoService>> _loggerMock;


    public TodoServiceTests()
    {
        _todoRepositoryMock = new Mock<ITodoRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _groupRepositoryMock = new Mock<IGroupRepository>();
        _notificationServiceMock = new Mock<INotificationService>();
        _cacheMock = new Mock<IMemoryCache>();
        _loggerMock = new Mock<ILogger<TodoService>>();
    }

    private NuggetDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<NuggetDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new NuggetDbContext(options);
    }

    private TodoService CreateService(NuggetDbContext context)
    {
        return new TodoService(
            context,
            _todoRepositoryMock.Object,
            _userRepositoryMock.Object,
            _groupRepositoryMock.Object,
            _notificationServiceMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetMyTodosAsync_ShouldReturnUserTodos()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);
        
        var userId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        
        var creator = new User 
        { 
            Id = creatorId, 
            Email = "admin@test.com", 
            Name = "Admin" 
        };
        context.Users.Add(creator);
        
        var todo = new Todo
        {
            Id = Guid.NewGuid(),
            Title = "Test Todo",
            Description = "Test Description",
            DueDate = DateTime.UtcNow.AddDays(5),
            CreatedById = creatorId,
            TargetType = TargetType.All
        };
        context.Todos.Add(todo);
        
        var assignment = new TodoAssignment
        {
            Id = Guid.NewGuid(),
            TodoId = todo.Id,
            UserId = userId,
            IsCompleted = false
        };
        context.TodoAssignments.Add(assignment);
        
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetMyTodosAsync(userId);

        // Assert
        Assert.Single(result);
        Assert.Equal("Test Todo", result[0].Title);
        Assert.False(result[0].IsCompleted);
    }

    [Fact]
    public async Task GetMyTodosAsync_ShouldFilterByCompletedStatus()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);
        
        var userId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        
        var creator = new User { Id = creatorId, Email = "admin@test.com", Name = "Admin" };
        context.Users.Add(creator);
        
        var completedTodo = new Todo
        {
            Id = Guid.NewGuid(),
            Title = "Completed Todo",
            DueDate = DateTime.UtcNow.AddDays(-1),
            CreatedById = creatorId,
            TargetType = TargetType.All
        };
        
        var incompleteTodo = new Todo
        {
            Id = Guid.NewGuid(),
            Title = "Incomplete Todo",
            DueDate = DateTime.UtcNow.AddDays(5),
            CreatedById = creatorId,
            TargetType = TargetType.All
        };
        
        context.Todos.AddRange(completedTodo, incompleteTodo);
        
        context.TodoAssignments.Add(new TodoAssignment
        {
            Id = Guid.NewGuid(),
            TodoId = completedTodo.Id,
            UserId = userId,
            IsCompleted = true,
            CompletedAt = DateTime.UtcNow
        });
        
        context.TodoAssignments.Add(new TodoAssignment
        {
            Id = Guid.NewGuid(),
            TodoId = incompleteTodo.Id,
            UserId = userId,
            IsCompleted = false
        });
        
        await context.SaveChangesAsync();

        // Act
        var completedResult = await service.GetMyTodosAsync(userId, isCompleted: true);
        var incompleteResult = await service.GetMyTodosAsync(userId, isCompleted: false);

        // Assert
        Assert.Single(completedResult);
        Assert.Equal("Completed Todo", completedResult[0].Title);
        
        Assert.Single(incompleteResult);
        Assert.Equal("Incomplete Todo", incompleteResult[0].Title);
    }

    [Fact]
    public async Task CreateTodoAsync_ShouldCreateTodoAndAssignToAllUsers()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);
        
        var creatorId = Guid.NewGuid();
        var request = new CreateTodoRequest
        {
            Title = "New Todo",
            Description = "Description",
            DueDate = DateTime.UtcNow.AddDays(7),
            TargetType = TargetType.All,
            NotifyImmediately = true
        };

        var allUsers = new List<User>
        {
            new User { Id = Guid.NewGuid(), Email = "user1@test.com", Name = "User 1" },
            new User { Id = Guid.NewGuid(), Email = "user2@test.com", Name = "User 2" }
        };

        _userRepositoryMock
            .Setup(r => r.GetAllActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allUsers);

        _todoRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Todo t, CancellationToken _) => t);

        // Act
        var result = await service.CreateTodoAsync(request, creatorId);

        // Assert
        Assert.Equal(request.Title, result.Title);
        Assert.Equal(2, result.Assignments.Count);
        
        _notificationServiceMock.Verify(
            n => n.SendNewTodoNotificationAsync(
                It.IsAny<Todo>(), 
                It.IsAny<IEnumerable<User>>(), 
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateTodoAsync_ShouldNotNotifyWhenNotifyImmediatelyIsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);
        
        var creatorId = Guid.NewGuid();
        var request = new CreateTodoRequest
        {
            Title = "Quiet Todo",
            DueDate = DateTime.UtcNow.AddDays(7),
            TargetType = TargetType.All,
            NotifyImmediately = false
        };

        _userRepositoryMock
            .Setup(r => r.GetAllActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        _todoRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Todo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Todo t, CancellationToken _) => t);

        // Act
        await service.CreateTodoAsync(request, creatorId);

        // Assert
        _notificationServiceMock.Verify(
            n => n.SendNewTodoNotificationAsync(
                It.IsAny<Todo>(), 
                It.IsAny<IEnumerable<User>>(), 
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CompleteTodoAsync_ShouldUpdateAssignment()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);
        
        var userId = Guid.NewGuid();
        var todoId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        
        var user = new User { Id = userId, Email = "user@test.com", Name = "User" };
        context.Users.Add(user);
        
        var todo = new Todo
        {
            Id = todoId,
            Title = "Test",
            DueDate = DateTime.UtcNow,
            CreatedById = Guid.NewGuid(),
            TargetType = TargetType.All
        };
        context.Todos.Add(todo);
        
        var assignment = new TodoAssignment
        {
            Id = assignmentId,
            TodoId = todoId,
            UserId = userId,
            IsCompleted = false
        };
        context.TodoAssignments.Add(assignment);
        
        await context.SaveChangesAsync();

        // Act
        var result = await service.CompleteTodoAsync(todoId, userId);

        // Assert
        Assert.True(result);
        
        var updatedAssignment = await context.TodoAssignments.FindAsync(assignmentId);
        Assert.NotNull(updatedAssignment);
        Assert.True(updatedAssignment.IsCompleted);
        Assert.NotNull(updatedAssignment.CompletedAt);
    }

    [Fact]
    public async Task CompleteTodoAsync_ShouldReturnFalseWhenAssignmentNotFound()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);
        
        var userId = Guid.NewGuid();
        var todoId = Guid.NewGuid();

        // Act
        var result = await service.CompleteTodoAsync(todoId, userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UncompleteTodoAsync_ShouldResetAssignment()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var service = CreateService(context);
        
        var userId = Guid.NewGuid();
        var todoId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        
        var user = new User { Id = userId, Email = "user@test.com", Name = "User" };
        context.Users.Add(user);
        
        var todo = new Todo
        {
            Id = todoId,
            Title = "Test",
            DueDate = DateTime.UtcNow,
            CreatedById = Guid.NewGuid(),
            TargetType = TargetType.All
        };
        context.Todos.Add(todo);
        
        var assignment = new TodoAssignment
        {
            Id = assignmentId,
            TodoId = todoId,
            UserId = userId,
            IsCompleted = true,
            CompletedAt = DateTime.UtcNow
        };
        context.TodoAssignments.Add(assignment);
        
        await context.SaveChangesAsync();

        // Act
        var result = await service.UncompleteTodoAsync(todoId, userId);

        // Assert
        Assert.True(result);
        
        var updatedAssignment = await context.TodoAssignments.FindAsync(assignmentId);
        Assert.NotNull(updatedAssignment);
        Assert.False(updatedAssignment.IsCompleted);
        Assert.Null(updatedAssignment.CompletedAt);
    }
}
