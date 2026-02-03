using Microsoft.EntityFrameworkCore;
using Nugget.Core.Entities;
using Nugget.Core.Interfaces;
using Nugget.Infrastructure.Data;

namespace Nugget.Infrastructure.Repositories;

/// <summary>
/// ToDoリポジトリ実装
/// </summary>
public class TodoRepository : ITodoRepository
{
    private readonly NuggetDbContext _context;

    public TodoRepository(NuggetDbContext context)
    {
        _context = context;
    }

    public async Task<Todo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Todos
            .Include(t => t.CreatedBy)
            .Include(t => t.Assignments)
                .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Todo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Todos
            .Include(t => t.CreatedBy)
            .Include(t => t.Assignments)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Todo> AddAsync(Todo todo, CancellationToken cancellationToken = default)
    {
        _context.Todos.Add(todo);
        await _context.SaveChangesAsync(cancellationToken);
        return todo;
    }

    public async Task UpdateAsync(Todo todo, CancellationToken cancellationToken = default)
    {
        _context.Todos.Update(todo);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Todo todo, CancellationToken cancellationToken = default)
    {
        _context.Todos.Remove(todo);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Todo>> GetTodosWithUpcomingDueDateAsync(int daysAhead, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var targetDate = today.AddDays(daysAhead);

        return await _context.Todos
            .Include(t => t.Assignments)
                .ThenInclude(a => a.User)
                    .ThenInclude(u => u.NotificationSetting)
            .Where(t => t.DueDate.Date <= targetDate && t.DueDate.Date >= today)
            .Where(t => t.Assignments.Any(a => !a.IsCompleted))
            .ToListAsync(cancellationToken);
    }
}
