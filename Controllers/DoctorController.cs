using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SuVanCop.Models;
using SuVanCop.Data;
namespace SuVanCop.Controllers;

public class DoctorController : Controller
{
    private readonly PostgresDbContext _context;

    public DoctorController(PostgresDbContext context)
    {
        _context = context;
    }
    

    public IActionResult Index()
    {
        var doctors = _context.doctors.ToList();
        return View(doctors);
    }

    public IActionResult Create([Bind("Names,LastNames,Nuip,Speciality")]Doctor doctor)
    {
        if (ModelState.IsValid)
            {
            _context.doctors.Add(doctor);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
            }
        return View(doctor);
            
    }

    
    [HttpGet]
    public IActionResult Edit(int id)
    {
        var doctor = _context.doctors.Find(id);
        return View(doctor);
    }
    [HttpPost]
    public IActionResult Edit(int id, [Bind("Names,LastNames,Nuip,Speciality")] Doctor updateDoctor)
    {
        var doctor = _context.doctors.Find(id);
        if (doctor == null)
        {
            return NotFound();
        }
        doctor.Names = updateDoctor.Names;
        doctor.LastNames = updateDoctor.LastNames;
        doctor.Nuip = updateDoctor.Nuip;
        doctor.Speciality = updateDoctor.Speciality;
        _context.doctors.Update(doctor);
        _context.SaveChanges();
        TempData["message"] = "Doctor editado exitosamente!";
        return RedirectToAction(nameof(Index));
        
    }
    
    public IActionResult Details(int id)
    {
        var doctor = _context.doctors.Find(id);
        return View(doctor);
    }
}