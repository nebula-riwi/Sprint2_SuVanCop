namespace SuVanCop.Models;

public class Turn
{
    public int Id { get; set; }
    public int Number { get; set; }
    public required string Type  { get; set; }
    public DateTime CreationDate { get; set; }
}