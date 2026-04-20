namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaseFlight.Infrastructure.Entity;

public sealed class BaseFlightEntity
{
    public int       Id         { get; set; }
    public string    FlightCode { get; set; } = null!;
    public int       AirlineId  { get; set; }
    public int       RouteId    { get; set; }
    public DateTime  CreatedAt  { get; set; }
    public DateTime? UpdatedAt  { get; set; }
}
