using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuVanCop.Data;
using SuVanCop.Models;

namespace SuVanCop.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly PostgresDbContext _context;

        public AppointmentController(PostgresDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var appointments = _context.appointments
                .Include(a => a.User)
                .Include(a => a.Doctor)
                .ToList();
            ViewBag.Doctors = _context.doctors.ToList();
            return View(appointments);
        }
        
        [HttpGet]
        public IActionResult GetUserByNuip(string nuip)
        {
            var user = _context.users.FirstOrDefault(u => u.Nuip == nuip);

            if (user == null)
            {
                return Json(new { found = false });
            }

            return Json(new
            {
                found = true,
                id = user.Id,
                name = user.Names + " " + user.LastNames
            });
        }

        [HttpPost]
        public IActionResult Create([Bind("Date,Hour,Status,UserId,DoctorId")] Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                appointment.Date = DateTime.SpecifyKind(appointment.Date, DateTimeKind.Utc);
                appointment.Hour = DateTime.SpecifyKind(appointment.Hour, DateTimeKind.Utc);
                _context.appointments.Add(appointment);
                _context.SaveChanges();

                TempData["message"] = "Cita creada exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Users = _context.users.ToList();
            ViewBag.Doctors = _context.doctors.ToList();
            return View(appointment);
        }
        
        
        public IActionResult Destroy(int id)
        {
            var appointment = _context.appointments.Find(id);
            if (appointment == null)
            {
                return NotFound();
            }
            _context.appointments.Remove(appointment);
            _context.SaveChanges();
            TempData["message"] = "Cita eliminada exitosamente!";
            return RedirectToAction(nameof(Index));
        }
    }
}
