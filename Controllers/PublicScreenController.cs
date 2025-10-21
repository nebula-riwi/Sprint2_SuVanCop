using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SuVanCop.Data;
using SuVanCop.Models.ViewModels;
using SuVanCop.Hubs;

namespace SuVanCop.Controllers;

public class PublicScreenController : Controller
{
    private readonly PostgresDbContext _context;
    private readonly IHubContext<TurnHub> _hubContext;

    public PublicScreenController(PostgresDbContext context, IHubContext<TurnHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        
        var activeTurn = await _context.turns
            .Where(t => t.Status == "active")
            .OrderBy(t => t.CreationDate)
            .FirstOrDefaultAsync();

        var currentMinute = now.Minute / 5 * 5; 
        var currentAppointment = await _context.appointments
            .Include(a => a.Doctor)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a =>
                a.Date.Date == now.Date &&  
                a.Hour.Hour == now.Hour &&  
                a.Hour.Minute == currentMinute 
            );

        var viewModel = new PublicScreenViewModel()
        {
            CurrentTurn = activeTurn,
            CurrentAppointment = currentAppointment
        };

        if (currentAppointment != null)
        {
            await _hubContext.Clients.All.SendAsync("UpdateAppointment", 
                currentAppointment.User?.FullName, 
                currentAppointment.Doctor?.FullName, 
                "Consultorio 1");
        }

        if (activeTurn != null)
        {
            Console.WriteLine($"Turno enviado: Number={activeTurn.Number}, Type={activeTurn.Type}, Type(typeof)={activeTurn.Type?.GetType()}");
            await _hubContext.Clients.All.SendAsync("UpdateTurn",
                0,
                activeTurn.Number,
                activeTurn.Type);
        }

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
