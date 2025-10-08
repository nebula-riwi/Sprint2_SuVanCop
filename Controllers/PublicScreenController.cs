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
        var now = DateTime.UtcNow;

        var activeTurn = _context.turns
            .Where(t => t.Status == "active")
            .OrderBy(t => t.CreationDate)
            .FirstOrDefault();


        var currentAppointment = _context.appointments
            .Include(a => a.Doctor)
            .Include(a => a.User)
            .FirstOrDefault(a =>
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

}

