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

    public IActionResult Destroy(int id)
    {
        var doctor = _context.doctors.Find(id);
        _context.doctors.Remove(doctor);
        _context.SaveChanges();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var doctor = _context.doctors.Find(id);
        return View(doctor);
    }
}