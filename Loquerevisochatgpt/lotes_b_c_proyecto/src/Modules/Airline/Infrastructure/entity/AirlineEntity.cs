namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Infrastructure.Entity;

public sealed class AirlineEntity
{
    public int       AirlineId { get; set; }
    public string    IataCode  { get; set; } = string.Empty;
    public string    Name      { get; set; } = string.Empty;
    public bool      IsActive  { get; set; }
    public DateTime  CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
