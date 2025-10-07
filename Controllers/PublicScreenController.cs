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

    public IActionResult Index()
    {
        var now = DateTime.Now;

        var activeTurns = _context.turns
            .Where(t => t.Status == "attending")
            .OrderBy(t => t.CreationDate)
            .ToList();

        var currentAppointment = _context.appointments
            .Include(a => a.Doctor)
            .Include(a => a.User)
            .FirstOrDefault(a =>
                a.Date == DateOnly.FromDateTime(now) &&
                a.Hour.Hours == now.Hour
            );

        var viewModel = new PublicScreenViewModel()
        {
            CurrentTurns = activeTurns,
            CurrentAppointment = currentAppointment
        };

        return View(viewModel);
    }
}
