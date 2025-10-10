using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SuVanCop.Data;
using SuVanCop.Hubs;
using SuVanCop.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace SuVanCop.Controllers
{
    public class ManageController : Controller
    {
        private readonly PostgresDbContext _context;
        private readonly IHubContext<TurnHub> _hubContext;

        public ManageController(PostgresDbContext context, IHubContext<TurnHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            var currentTurn = await _context.turns
                .Where(t => t.Status == "active" || t.Status == "pending")
                .OrderBy(t => t.CreationDate)
                .FirstOrDefaultAsync();

            if (currentTurn == null || currentTurn.Status != "active")
            {
                var firstPendingTurn = await _context.turns
                    .Where(t => t.Status == "pending")
                    .OrderBy(t => t.CreationDate)
                    .FirstOrDefaultAsync();

                if (firstPendingTurn != null)
                {
                    firstPendingTurn.Status = "active";
                    _context.Update(firstPendingTurn);
                    await _context.SaveChangesAsync();

                    await _hubContext.Clients.All.SendAsync("UpdateTurn",
                        firstPendingTurn.Id,
                        firstPendingTurn.Number,
                        firstPendingTurn.Type);
                    
                    currentTurn = firstPendingTurn;
                }
            }

            var viewModel = new ManageViewModel()
            {
                CurrentTurn = currentTurn
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeTurn(int currentTurnId)
        {
            var currentTurn = await _context.turns
                .FirstOrDefaultAsync(t => t.Id == currentTurnId);

            if (currentTurn != null)
            {
                if (currentTurn.Status == "active")
                {
                    currentTurn.Status = "completed";
                    _context.Update(currentTurn);
                    await _context.SaveChangesAsync();
                }

                var nextTurn = await _context.turns
                    .Where(t => t.Status == "pending")
                    .OrderBy(t => t.CreationDate)
                    .FirstOrDefaultAsync();

                if (nextTurn != null)
                {
                    nextTurn.Status = "active";
                    _context.Update(nextTurn);
                    await _context.SaveChangesAsync();

                    await _hubContext.Clients.All.SendAsync("UpdateTurn", nextTurn.Id, nextTurn.Number, nextTurn.Type ?? "NoType");
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("UpdateTurn", 0, "N/A");
                }
            }

            return Ok();
        }
    }
}
