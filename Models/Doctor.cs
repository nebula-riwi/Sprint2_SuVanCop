namespace SuVanCop.Models;

public class Doctor
{
    public int Id { get; set; }
    public string Names { get; set; }
    public string LastNames { get; set; }
    public string Nuip { get; set; }
    public string Speciality { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    
    public string FullName => $"{Names} {LastNames}";
}