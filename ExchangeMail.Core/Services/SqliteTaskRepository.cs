using ExchangeMail.Core.Data;
using ExchangeMail.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMail.Core.Services;

public class SqliteTaskRepository : ITaskRepository
{
    private readonly ExchangeMailContext _context;

    public SqliteTaskRepository(ExchangeMailContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaskEntity>> GetTasksAsync(string userEmail, bool includeCompleted = false)
    {
        var query = _context.Tasks.Where(t => t.UserEmail == userEmail);

        if (!includeCompleted)
        {
            query = query.Where(t => !t.IsCompleted);
        }

        // Order by DueDate (nulls last), then Priority (High to Low), then Id
        return await query
            .OrderBy(t => t.IsCompleted) // Completed last
            .ThenBy(t => t.DueDate.HasValue ? 0 : 1) // Due dates first
            .ThenBy(t => t.DueDate)
            .ThenByDescending(t => t.Priority)
            .ToListAsync();
    }

    public async Task<TaskEntity?> GetTaskAsync(int id)
    {
        return await _context.Tasks.FindAsync(id);
    }

    public async Task AddTaskAsync(TaskEntity task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateTaskAsync(TaskEntity task)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteTaskAsync(int id)
    {
        var entity = await _context.Tasks.FindAsync(id);
        if (entity != null)
        {
            _context.Tasks.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
