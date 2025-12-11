using ExchangeMail.Core.Data;
using ExchangeMail.Core.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMail.Web.Controllers;

public class ContactsController : Controller
{
    private readonly ExchangeMailContext _context;

    public ContactsController(ExchangeMailContext context)
    {
        _context = context;
    }

    private string? GetCurrentUser() => User.Identity?.Name;

    public async Task<IActionResult> Index()
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login", "Mail");
        return View(await _context.Contacts.ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login", "Mail");
        if (id == null)
        {
            return NotFound();
        }

        var contact = await _context.Contacts
            .FirstOrDefaultAsync(m => m.Id == id);
        if (contact == null)
        {
            return NotFound();
        }

        return View(contact);
    }

    public async Task<IActionResult> DetailsPartial(int? id)
    {
        if (GetCurrentUser() == null) return Unauthorized();
        if (id == null) return NotFound();

        var contact = await _context.Contacts.FindAsync(id);
        if (contact == null) return NotFound();

        return PartialView("_ContactDetails", contact);
    }

    public IActionResult Create(string? name, string? email)
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login", "Mail");
        if (!string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(email))
        {
            return View(new ContactEntity { Name = name ?? "", Email = email ?? "" });
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Name,Email,PhoneNumber,Address,Notes")] ContactEntity contact)
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login", "Mail");
        if (ModelState.IsValid)
        {
            _context.Add(contact);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(contact);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login", "Mail");
        if (id == null)
        {
            return NotFound();
        }

        var contact = await _context.Contacts.FindAsync(id);
        if (contact == null)
        {
            return NotFound();
        }
        return View(contact);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,PhoneNumber,Address,Notes")] ContactEntity contact)
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login", "Mail");
        if (id != contact.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(contact);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ContactExists(contact.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(contact);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login", "Mail");
        if (id == null)
        {
            return NotFound();
        }

        var contact = await _context.Contacts
            .FirstOrDefaultAsync(m => m.Id == id);
        if (contact == null)
        {
            return NotFound();
        }

        return View(contact);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (GetCurrentUser() == null) return RedirectToAction("Login", "Mail");
        var contact = await _context.Contacts.FindAsync(id);
        if (contact != null)
        {
            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool ContactExists(int id)
    {
        return _context.Contacts.Any(e => e.Id == id);
    }
}
