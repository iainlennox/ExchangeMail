using ExchangeMail.Core.Data.Entities;

namespace ExchangeMail.Core.Services;

public interface ITaskRepository
{
    Task<IEnumerable<TaskEntity>> GetTasksAsync(string userEmail, bool includeCompleted = false);
    Task<TaskEntity?> GetTaskAsync(int id);
    Task AddTaskAsync(TaskEntity task);
    Task UpdateTaskAsync(TaskEntity task);
    Task DeleteTaskAsync(int id);
}
