namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftManufacturer.Infrastructure.Entity;

public sealed class AircraftManufacturerEntity
{
    public int    ManufacturerId { get; set; }
    public string Name          { get; set; } = string.Empty;
    public int    CountryId     { get; set; }
}
