namespace SuVanCop.Models;


    public class User
    {
        public int Id { get; set; }
        public required string Names { get; set; }
        public required string LastNames { get; set; }
        public required string Nuip { get; set; }
        
        public required string Rh { get; set; }
        public required string PictureUrl { get; set; }
        
        public required string Status  { get; set; }
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        
        public string FullName => $"{Names} {LastNames}";
       
    }
