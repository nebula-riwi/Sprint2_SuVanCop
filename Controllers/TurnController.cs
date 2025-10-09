using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuVanCop.Models;
using SuVanCop.Data;

namespace SuVanCop.Controllers;

public class TurnController : Controller
{
    private readonly PostgresDbContext _context;

    public TurnController(PostgresDbContext context)
    {
        _context = context;
    }
    public async Task<IActionResult> Index()
    {

        var turns = await _context.turns
            .Where(t => t.Status != "OldCycle")
            .OrderBy(t => t.CreationDate)
            .ToListAsync();

        return View(turns);
    }
 
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTurn(string type)
    {
        if (string.IsNullOrEmpty(type) || type.Length != 1)
        {
            return BadRequest("Tipo de turno inválido.");
        }

        int newNumber;

        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                var lastTurn = await _context.turns
                    .Where(t => t.Type == type && t.Status != "OldCycle")
                    .OrderByDescending(t => t.CreationDate)
                    .FirstOrDefaultAsync();

                newNumber = (lastTurn != null) ? lastTurn.Number + 1 : 1;

                var newTurn = new Turn
                {
                    Number = newNumber,
                    Type = type,
                    CreationDate = DateTime.Now,
                    Status = "pending"
                };

                _context.turns.Add(newTurn);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                string fullCode = $"{newTurn.Type}-{newTurn.Number:D3}";

                return RedirectToAction("TurnGenerated", new { code = fullCode });
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Error interno al generar el turno.");
            }
        }
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetCounter(string type)
    {
        if (string.IsNullOrEmpty(type) || type.Length != 1)
        {
            return BadRequest("Tipo de turno inválido.");
        }
        var oldTurns = await _context.turns
                                     .Where(t => t.Type == type && t.Status != "OldCycle")
                                     .ToListAsync();

        foreach (var turn in oldTurns)
        {
            turn.Status = "OldCycle";
        }

        _context.turns.UpdateRange(oldTurns);
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }
 
    public IActionResult TurnGenerated(string code)
    {
        ViewData["TurnCode"] = code;
        return View();
    }

}
