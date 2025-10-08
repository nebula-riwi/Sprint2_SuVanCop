using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SuVanCop.Data;
using SuVanCop.Hubs;
using SuVanCop.Models.ViewModels;

namespace SuVanCop.Controllers;

public class ManageController : Controller
{
    private readonly PostgresDbContext _context;
    private readonly IHubContext<TurnHub> _hubContext;

    public ManageController(PostgresDbContext context, IHubContext<TurnHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }
    public IActionResult Index()
    {
        var currentTurn = _context.turns
            .Where(t => t.Status == "active" || t.Status == "pending")
            .OrderBy(t => t.CreationDate)
            .FirstOrDefault();


        var viewModel = new ManageViewModel()
        {
            CurrentTurn = currentTurn
        };

        return View(viewModel);
    }
    
    [HttpPost]
    public async Task<IActionResult> ChangeTurn(int currentTurnId)
    {
        var currentTurn = _context.turns.FirstOrDefault(t => t.Id == currentTurnId);

        if (currentTurn != null)
        {
            if (currentTurn.Status == "active")
            {
                currentTurn.Status = "completed";
                _context.Update(currentTurn);
            }

            var nextTurn = _context.turns
                .Where(t => t.Status == "pending")
                .OrderBy(t => t.CreationDate)
                .FirstOrDefault();

            if (nextTurn != null)
            {
                nextTurn.Status = "active";
                _context.Update(nextTurn);

                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("UpdateTurn", nextTurn.Id, nextTurn.Number, nextTurn.Type);
            }
            else
            {
                await _context.SaveChangesAsync();
                await _hubContext.Clients.All.SendAsync("UpdateTurn", 0, "0", "NoType");
            }
        }

        Console.WriteLine($"📥 Cambiando turno: ID = {currentTurnId}");

        return Ok();
    }

}