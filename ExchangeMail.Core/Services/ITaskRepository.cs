using ExchangeMail.Core.Data.Entities;

namespace ExchangeMail.Core.Services;

public interface ITaskRepository
{
    Task<IEnumerable<TaskEntity>> GetTasksAsync(string userEmail, string? filterType = "active", string? searchQuery = null, string? sortBy = null, bool sortDesc = false);
    Task<TaskEntity?> GetTaskAsync(int id);
    Task AddTaskAsync(TaskEntity task);
    Task UpdateTaskAsync(TaskEntity task);
    Task DeleteTaskAsync(int id);
}
