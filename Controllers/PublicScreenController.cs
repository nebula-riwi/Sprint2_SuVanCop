using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuVanCop.Data;
using SuVanCop.Models.ViewModels;

namespace SuVanCop.Controllers;

public class PublicScreenController : Controller
{
    private readonly PostgresDbContext _context;

    public PublicScreenController(PostgresDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;

        var activeTurn = await _context.turns
            .Where(t => t.Status == "active")
            .OrderBy(t => t.CreationDate)
            .FirstOrDefaultAsync();

        var currentAppointment = await _context.appointments
            .Include(a => a.Doctor)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a =>
                a.Date == now.Date &&
                a.Hour.Hour == now.Hour
            );

        var viewModel = new PublicScreenViewModel()
        {
            CurrentTurn = activeTurn,
            CurrentAppointment = currentAppointment
        };

        return View(viewModel);
    }
    [HttpGet]
    public async Task<IActionResult> GetUpdatedTurn()
    {
        var activeTurn = await _context.turns
            .Where(t => t.Status == "active")
            .OrderBy(t => t.CreationDate)
            .FirstOrDefaultAsync();

        if (activeTurn != null)
        {
            return Json(new { number = activeTurn.Number, type = activeTurn.Type });
        }

        return Json(new { number = 0, type = "N/A" });
    }

}

