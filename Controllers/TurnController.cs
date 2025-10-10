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
    public IActionResult CreateTurn(string type)
    {
        
        if (string.IsNullOrEmpty(type) || type.Length != 1)
        {
            return BadRequest("Tipo de turno inválido.");
        }

        int newNumber;

        // 2. Transacción de Base de Datos Síncrona
        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                // A. Buscar el último turno de forma SÍNCRONA
                // *** USANDO: _context.turns ***
                var lastTurn = _context.turns 
                    .Where(t => t.Type == type && t.Status != "OldCycle")
                    .OrderByDescending(t => t.CreationDate)
                    .FirstOrDefault(); 

                // B. Calcular el nuevo número
                newNumber = (lastTurn != null) ? lastTurn.Number + 1 : 1;

                // C. Crear el nuevo Turno
                var newTurn = new Turn
                {
                    Number = newNumber,
                    Type = type,
                    CreationDate = DateTime.Now,
                    Status = "pending"
                };

                // D. Guardar cambios
                _context.turns.Add(newTurn); // Usando _context.turns
                _context.SaveChanges(); 
                transaction.Commit(); 

                // 3. Formatear y Redirigir
                string fullCode = $"{newTurn.Type}-{newTurn.Number:D3}";
                
                // Si necesitas imprimir, la llamada iría aquí, asegurándote que ImprimirTicketTurno sea síncrona:
                // ImprimirTicketTurno(newTurn.Type, newTurn.Number); 

                return RedirectToAction("TurnGenerated", new { code = fullCode });
            }
            catch (Exception)
            {
                // Manejo de error
                transaction.Rollback();
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
