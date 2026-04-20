namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Infrastructure.Entity;

/// <summary>
/// Entidad EF Core para la tabla <c>city</c>.
/// UseSnakeCaseNamingConvention() mapea automáticamente los nombres.
/// </summary>
public sealed class CityEntity
{
    public int      CityId    { get; set; }
    public string   Name      { get; set; } = string.Empty;
    public int      CountryId { get; set; }
    public DateTime CreatedAt { get; set; }
}
