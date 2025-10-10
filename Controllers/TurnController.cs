using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
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
        using (var transaction = _context.Database.BeginTransaction())
        {
            try
            {
                var lastTurn = _context.turns 
                    .Where(t => t.Type == type && t.Status != "OldCycle")
                    .OrderByDescending(t => t.CreationDate)
                    .FirstOrDefault(); 
                newNumber = (lastTurn != null) ? lastTurn.Number + 1 : 1;
                var creationDate =DateTime.Now;
                var newTurn = new Turn
                {
                    Number = newNumber,
                    Type = type,
                    CreationDate = DateTime.SpecifyKind(creationDate,DateTimeKind.Utc),
                    Status = "pending"
                };
                Console.WriteLine(newTurn);

                _context.turns.Add(newTurn);
                _context.SaveChanges(); 
                transaction.Commit(); 

                string fullCode = $"{newTurn.Type}-{newTurn.Number:D3}";
                ImprimirTicketTurno(newTurn.Type, newTurn.Number); 
            
                // USAMOS TEMPDATA para almacenar el código ANTES de redirigir
                TempData["SuccessMessage"] = $"Turno {fullCode} creado e impreso correctamente.";
                TempData["TurnCode"] = fullCode;

                // Redirección al Index
                return RedirectToAction(nameof(Index)); 
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                // Usamos TempData para el mensaje de error
                TempData["ErrorMessage"] = "Error al generar e imprimir el turno. Por favor, intente de nuevo o notifique al personal.";
                return RedirectToAction(nameof(Index)); 
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
    
  public void ImprimirTicketTurno(string type, int number)
{
    // Código de turno completo (e.g., G-001)
    string fullCode = $"{type}-{number.ToString("D3")}";

    // La hora local para imprimir en el ticket
    DateTime now = DateTime.Now;

    // --- Configuración de la Impresora ---
    PrintDocument pd = new PrintDocument();
    pd.PrinterSettings.PrinterName = "XP-58"; // Verifica el nombre

    pd.PrintPage += (sender, e) =>
    {
        Graphics g = e.Graphics;
        int width = 220; // Ancho del papel (ajusta si usas 80mm)
        int yPos = 10;   // Posición inicial Y
        const int SHIFT_LEFT = -10; // Desplazamiento de 10px a la izquierda

        // Funciones de ayuda para centrar texto
        Func<string, Font, float> CenterText = (text, font) => 
            (width - g.MeasureString(text, font).Width) / 2;

        // --- 1. CABECERA (LOGOTIPO) ---
        try
        {
            // Descargar el logo de forma síncrona
            using var httpLogo = new HttpClient();
            // ADVERTENCIA: Usar .Result bloquea, pero es necesario para mantener la función síncrona.
            var logoBytes = httpLogo.GetByteArrayAsync("https://i.ibb.co/Qv3zm29F/image-1759875201459-removebg-preview.png").Result;
            using var logoStream = new MemoryStream(logoBytes);
            var logo = Image.FromStream(logoStream);

            int logoWidth = 120;
            int logoHeight = 90;
            
            // Cálculo: (Ancho total - Ancho del logo) / 2 + Desplazamiento (-10)
            int logoX = ((width - logoWidth) / 2) + SHIFT_LEFT;
            
            g.DrawImage(logo, logoX, yPos, logoWidth, logoHeight);
            yPos += logoHeight + 15;
        }
        catch (Exception ex)
        {
             // En caso de error, usa texto de respaldo
            string fallbackText = "Error logo / SUVANC0P";
            using var fallbackFont = new Font("Arial", 12, FontStyle.Bold);
            g.DrawString(fallbackText, fallbackFont, Brushes.Black, CenterText(fallbackText, fallbackFont) + SHIFT_LEFT, yPos);
            yPos += (int)fallbackFont.GetHeight() + 15;
        }
        
        g.DrawLine(Pens.Black, 5, yPos, width - 5, yPos);
        yPos += 15; // Espacio estándar después de la línea
        
        // *** AQUI SE AÑADEN LOS 40px DE MARGEN SOLICITADOS ***
        yPos += 40; 
        
        // --- 2. HORA Y FECHA DE EMISIÓN ---
        
        string dateString = $"Fecha: {now:dd/MM/yyyy}";
        string timeString = $"Hora: {now:hh:mm:ss tt}"; // Incluye AM/PM (tt)

        using var smallFont = new Font("Consolas", 9, FontStyle.Regular);
        
        // Dibujar fecha y hora, centradas.
        g.DrawString(dateString, smallFont, Brushes.Black, CenterText(dateString, smallFont), yPos);
        yPos += (int)smallFont.GetHeight() + 2;
        g.DrawString(timeString, smallFont, Brushes.Black, CenterText(timeString, smallFont), yPos);
        yPos += (int)smallFont.GetHeight() + 25; // Espacio antes del código

        // --- 3. CÓDIGO DE TURNO (EL PROTAGONISTA) ---

        // Texto de indicación
        string label = "SU TURNO ES";
        using var labelFont = new Font("Arial", 14, FontStyle.Bold);
        g.DrawString(label, labelFont, Brushes.Black, CenterText(label, labelFont), yPos);
        yPos += (int)labelFont.GetHeight() + 5;

        // CÓDIGO GRANDE y GRUESO
        using var codeFont = new Font("Arial", 40, FontStyle.Bold); // ¡Más grande!
        g.DrawString(fullCode, codeFont, Brushes.Black, CenterText(fullCode, codeFont), yPos);
        yPos += (int)codeFont.GetHeight() + 30; // Gran espacio después del código

        // --- 4. PIE DE PÁGINA Y MENSAJE FINAL ---
        
        string message = "Esté atento a su llamado.";
        using var footerFont = new Font("Arial", 10, FontStyle.Italic);
        g.DrawString(message, footerFont, Brushes.Black, CenterText(message, footerFont), yPos);
        yPos += (int)footerFont.GetHeight() + 25;

        // Línea de corte (para que la impresora sepa dónde cortar)
        g.DrawLine(Pens.Black, 5, yPos, width - 5, yPos);
        yPos += 20; // Espacio para el corte

        e.HasMorePages = false;
    };

    pd.Print();
}
}