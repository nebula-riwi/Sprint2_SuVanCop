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

            ViewBag.Doctors = _context.doctors.Where(d => d.Status == "Activo").ToList();
            return View(appointments);
        }

        public IActionResult Details(int id)
        {
            var appointment = _context.appointments
                .Include(a => a.User)
                .Include(a => a.Doctor)
                .FirstOrDefault(a => a.Id == id);

            if (appointment == null)
            {
                TempData["message"] = "Cita no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            return View(appointment);
        }

        [HttpGet]
        public IActionResult GetUserByNuip(string nuip)
        {
            var user = _context.users.FirstOrDefault(u => u.Nuip == nuip);

            if (user == null)
                return Json(new { found = false });

            return Json(new
            {
                found = true,
                id = user.Id,
                name = user.Names + " " + user.LastNames
            });
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Users = _context.users.ToList();
            ViewBag.Doctors = _context.doctors.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult Create([Bind("Date,Hour,Status,UserId,DoctorId")] Appointment appointment)
        {
            Console.WriteLine($"Date: {appointment.Date}, Hour: {appointment.Hour}, UserId: {appointment.UserId}, DoctorId: {appointment.DoctorId}, Status: {appointment.Status}");

            var user = _context.users.FirstOrDefault(u => u.Id == appointment.UserId);
            if (user == null)
            {
                TempData["error"] = "Usuario no encontrado";
                return RedirectToAction(nameof(Index));
            }

            if (user.Status != "Activo")
            {
                TempData["error"] = "El usuario no se encuentra activo";
                return RedirectToAction(nameof(Index));
            }

            // 🔹 Convertir la hora (string/TimeSpan) a DateTime con la fecha seleccionada
            if (TimeSpan.TryParse(appointment.Hour.ToString(), out var horaSeleccionada))
            {
                appointment.Hour = appointment.Date.Date.Add(horaSeleccionada);
            }

            // 🔹 Normalizar ambas fechas a UTC para PostgreSQL
            appointment.Date = DateTime.SpecifyKind(appointment.Date, DateTimeKind.Utc);
            appointment.Hour = DateTime.SpecifyKind(appointment.Hour, DateTimeKind.Utc);

            // 🔹 Verificar disponibilidad (sin usar .Date en la consulta)
            var startOfDay = appointment.Date.Date;
            var endOfDay = startOfDay.AddDays(1);

            bool citaExistente = _context.appointments.Any(a =>
                a.DoctorId == appointment.DoctorId &&
                a.Date >= startOfDay && a.Date < endOfDay &&
                a.Hour == appointment.Hour
            );

            if (citaExistente)
            {
                TempData["error"] = "El doctor ya tiene una cita programada en esa fecha y hora.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                _context.appointments.Add(appointment);
                _context.SaveChanges();

                TempData["message"] = "Cita creada exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Users = _context.users.ToList();
            ViewBag.Doctors = _context.doctors.ToList();
            return View(appointment);
        }

        public IActionResult Edit(int id, [Bind("Date,Hour")] Appointment updateAppointment)
        {
            var appointment = _context.appointments.Find(id);

            if (appointment == null)
            {
                TempData["error"] = "Usuario no encontrado";
                return RedirectToAction(nameof(Index));
            }

            appointment.Date = DateTime.SpecifyKind(updateAppointment.Date, DateTimeKind.Utc);
            appointment.Hour = DateTime.SpecifyKind(updateAppointment.Hour, DateTimeKind.Utc);
            _context.appointments.Update(appointment);
            _context.SaveChanges();

            TempData["message"] = "Cita editada.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var appointment = _context.appointments
                .Include(a => a.User)
                .Include(a => a.Doctor)
                .FirstOrDefault(a => a.Id == id);

            if (appointment == null)
            {
                TempData["error"] = "Cita no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Doctors = _context.doctors.ToList();
            return View(appointment);
        }

        [HttpGet]
        public IActionResult ValidateAvailability(int doctorId, DateTime date, string hour)
        {
            // 🔹 Asegurar que el DateTime sea UTC
            if (date.Kind == DateTimeKind.Unspecified)
                date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

            // 🔹 Rango del día completo
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            var appointments = _context.appointments
                .Where(a => a.DoctorId == doctorId && a.Date >= startOfDay && a.Date < endOfDay)
                .AsEnumerable();

            bool available = !appointments.Any(a => a.Hour.ToString("HH:mm") == hour);

            return Json(new { available });
        }
    }
}
