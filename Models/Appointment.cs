namespace SuVanCop.Models;

public class Appointment
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public DateTime Hour { get; set; }
    public required string Status {get; set;}
    public int? UserId  { get; set; }
    public User? User { get; set; }
    public int? DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    
    
}