namespace SuVanCop.Models;

public class Appointment
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeSpan Hour { get; set; }
    public string Status {get; set;}
    public int? UserId  { get; set; }
    public User? User { get; set; }
    public int? DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    
    
}