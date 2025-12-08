using ExchangeMail.Core.Data;
using ExchangeMail.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMail.Core.Services;

public class SqliteCalendarRepository : ICalendarRepository
{
    private readonly ExchangeMailContext _context;

    public SqliteCalendarRepository(ExchangeMailContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CalendarEventEntity>> GetEventsAsync(string userEmail, DateTime start, DateTime end)
    {
        return await _context.CalendarEvents
            .Where(e => e.UserEmail == userEmail && e.StartDateTime < end && e.EndDateTime > start)
            .ToListAsync();
    }

    public async Task<CalendarEventEntity?> GetEventAsync(int id)
    {
        return await _context.CalendarEvents.FindAsync(id);
    }

    public async Task AddEventAsync(CalendarEventEntity calendarEvent)
    {
        _context.CalendarEvents.Add(calendarEvent);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateEventAsync(CalendarEventEntity calendarEvent)
    {
        _context.CalendarEvents.Update(calendarEvent);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteEventAsync(int id)
    {
        var entity = await _context.CalendarEvents.FindAsync(id);
        if (entity != null)
        {
            _context.CalendarEvents.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
