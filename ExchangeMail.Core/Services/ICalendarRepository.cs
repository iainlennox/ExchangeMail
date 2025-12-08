using ExchangeMail.Core.Data.Entities;

namespace ExchangeMail.Core.Services;

public interface ICalendarRepository
{
    Task<IEnumerable<CalendarEventEntity>> GetEventsAsync(string userEmail, DateTime start, DateTime end);
    Task<CalendarEventEntity?> GetEventAsync(int id);
    Task AddEventAsync(CalendarEventEntity calendarEvent);
    Task UpdateEventAsync(CalendarEventEntity calendarEvent);
    Task DeleteEventAsync(int id);
}
