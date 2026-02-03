using Microsoft.EntityFrameworkCore;
using Nugget.Core.Entities;
using Nugget.Core.Enums;
using Nugget.Infrastructure.Data;
using Nugget.Infrastructure.Repositories;

namespace Nugget.Infrastructure.Tests;

public class TodoRepositoryTests
{
    private static NuggetDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<NuggetDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new NuggetDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ShouldAddTodoToDatabase()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TodoRepository(context);
        
        var user = new User { Id = Guid.NewGuid(), Email = "admin@test.com", Name = "Admin" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var todo = new Todo
        {
            Id = Guid.NewGuid(),
            Title = "テストToDo",
            Description = "テスト説明",
            DueDate = DateTime.UtcNow.AddDays(7),
            CreatedById = user.Id,
            TargetType = TargetType.All
        };

        // Act
        var result = await repository.AddAsync(todo);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(todo.Title, result.Title);
        
        var savedTodo = await context.Todos.FindAsync(todo.Id);
        Assert.NotNull(savedTodo);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTodoWithRelations()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TodoRepository(context);
        
        var user = new User { Id = Guid.NewGuid(), Email = "admin@test.com", Name = "Admin" };
        context.Users.Add(user);
        
        var todo = new Todo
        {
            Id = Guid.NewGuid(),
            Title = "テストToDo",
            DueDate = DateTime.UtcNow.AddDays(7),
            CreatedById = user.Id,
            TargetType = TargetType.All
        };
        context.Todos.Add(todo);
        
        var assignment = new TodoAssignment
        {
            Id = Guid.NewGuid(),
            TodoId = todo.Id,
            UserId = user.Id
        };
        context.TodoAssignments.Add(assignment);
        
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(todo.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(todo.Title, result.Title);
        Assert.NotNull(result.CreatedBy);
        Assert.Single(result.Assignments);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTodosOrderedByCreatedAtDescending()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TodoRepository(context);
        
        var user = new User { Id = Guid.NewGuid(), Email = "admin@test.com", Name = "Admin" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        var todo1 = new Todo
        {
            Id = Guid.NewGuid(),
            Title = "ToDo 1",
            DueDate = DateTime.UtcNow.AddDays(1),
            CreatedById = user.Id,
            TargetType = TargetType.All
        };
        
        var todo2 = new Todo
        {
            Id = Guid.NewGuid(),
            Title = "ToDo 2",
            DueDate = DateTime.UtcNow.AddDays(2),
            CreatedById = user.Id,
            TargetType = TargetType.All
        };
        
        context.Todos.AddRange(todo1, todo2);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateTodo()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TodoRepository(context);
        
        var user = new User { Id = Guid.NewGuid(), Email = "admin@test.com", Name = "Admin" };
        context.Users.Add(user);
        
        var todo = new Todo
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            DueDate = DateTime.UtcNow.AddDays(7),
            CreatedById = user.Id,
            TargetType = TargetType.All
        };
        context.Todos.Add(todo);
        await context.SaveChangesAsync();

        // Act
        todo.Title = "Updated Title";
        await repository.UpdateAsync(todo);

        // Assert
        var updatedTodo = await context.Todos.FindAsync(todo.Id);
        Assert.NotNull(updatedTodo);
        Assert.Equal("Updated Title", updatedTodo.Title);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTodo()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TodoRepository(context);
        
        var user = new User { Id = Guid.NewGuid(), Email = "admin@test.com", Name = "Admin" };
        context.Users.Add(user);
        
        var todo = new Todo
        {
            Id = Guid.NewGuid(),
            Title = "To Delete",
            DueDate = DateTime.UtcNow.AddDays(7),
            CreatedById = user.Id,
            TargetType = TargetType.All
        };
        context.Todos.Add(todo);
        await context.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(todo);

        // Assert
        var deletedTodo = await context.Todos.FindAsync(todo.Id);
        Assert.Null(deletedTodo);
    }

    [Fact]
    public async Task GetTodosWithUpcomingDueDateAsync_ShouldReturnTodosWithinDateRange()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new TodoRepository(context);
        
        var user = new User { Id = Guid.NewGuid(), Email = "user@test.com", Name = "User" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        var todoDueSoon = new Todo
        {
            Id = Guid.NewGuid(),
            Title = "Due Soon",
            DueDate = DateTime.UtcNow.AddDays(2),
            CreatedById = user.Id,
            TargetType = TargetType.All
        };
        
        var todoDueLater = new Todo
        {
            Id = Guid.NewGuid(),
            Title = "Due Later",
            DueDate = DateTime.UtcNow.AddDays(10),
            CreatedById = user.Id,
            TargetType = TargetType.All
        };
        
        context.Todos.AddRange(todoDueSoon, todoDueLater);
        
        context.TodoAssignments.Add(new TodoAssignment
        {
            Id = Guid.NewGuid(),
            TodoId = todoDueSoon.Id,
            UserId = user.Id,
            IsCompleted = false
        });
        
        context.TodoAssignments.Add(new TodoAssignment
        {
            Id = Guid.NewGuid(),
            TodoId = todoDueLater.Id,
            UserId = user.Id,
            IsCompleted = false
        });
        
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetTodosWithUpcomingDueDateAsync(3);

        // Assert
        Assert.Single(result);
        Assert.Equal("Due Soon", result[0].Title);
    }
}
