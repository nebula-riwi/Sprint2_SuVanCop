namespace SuVanCop.Models;


    public class User
    {
        public int Id { get; set; }
        public string Names { get; set; }
        public string LastNames { get; set; }
        public string Nuip { get; set; }
        
        public string Rh { get; set; }
        public string PictureUrl { get; set; }
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
       
    }
