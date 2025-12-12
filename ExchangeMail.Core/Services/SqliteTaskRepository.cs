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

    public async Task<IEnumerable<TaskEntity>> GetTasksAsync(string userEmail, string? filterType = "active", string? searchQuery = null, string? sortBy = null, bool sortDesc = false)
    {
        var query = _context.Tasks.Where(t => t.UserEmail == userEmail);

        // Filter Logic
        switch (filterType?.ToLower())
        {
            case "completed":
                query = query.Where(t => t.Status == Data.Entities.TaskStatus.Completed);
                break;
            case "waiting":
                query = query.Where(t => t.Status == Data.Entities.TaskStatus.Waiting);
                break;
            case "blocked":
                query = query.Where(t => t.Status == Data.Entities.TaskStatus.Blocked);
                break;
            case "email":
                query = query.Where(t => t.Origin == "Email" && t.Status != Data.Entities.TaskStatus.Completed);
                break;
            case "all":
                // No filter, return everything
                break;
            case "active":
            default:
                // Show all non-completed
                query = query.Where(t => t.Status != Data.Entities.TaskStatus.Completed);
                break;
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            searchQuery = searchQuery.ToLower();
            query = query.Where(t => t.Subject.ToLower().Contains(searchQuery) ||
                                     (t.Description != null && t.Description.ToLower().Contains(searchQuery)) ||
                                     (t.Tags != null && t.Tags.ToLower().Contains(searchQuery)));
        }

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "priority" => sortDesc ? query.OrderByDescending(t => t.Priority) : query.OrderBy(t => t.Priority),
            "date" => sortDesc ? query.OrderByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate),
            "created" => sortDesc ? query.OrderByDescending(t => t.Id) : query.OrderBy(t => t.Id), // Proxy for created
            _ => query // Default below
        };

        if (string.IsNullOrEmpty(sortBy))
        {
            // Default Sort: DueDate, then Priority, then Id
            return await query
               .OrderBy(t => t.DueDate.HasValue ? 0 : 1)
               .ThenBy(t => t.DueDate)
               .ThenByDescending(t => t.Priority)
               .ToListAsync();
        }

        return await query.ToListAsync();
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
