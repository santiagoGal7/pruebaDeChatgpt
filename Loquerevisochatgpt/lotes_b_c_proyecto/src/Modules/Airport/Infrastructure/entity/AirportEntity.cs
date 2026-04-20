namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airport.Infrastructure.Entity;

/// <summary>
/// Entidad EF Core para la tabla <c>airport</c>.
/// </summary>
public sealed class AirportEntity
{
    public int      AirportId { get; set; }
    public string   IataCode  { get; set; } = string.Empty;
    public string   Name      { get; set; } = string.Empty;
    public int      CityId    { get; set; }
    public DateTime CreatedAt { get; set; }
}
