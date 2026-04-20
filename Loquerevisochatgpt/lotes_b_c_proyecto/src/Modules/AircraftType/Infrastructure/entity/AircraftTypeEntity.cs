namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.AircraftType.Infrastructure.Entity;

public sealed class AircraftTypeEntity
{
    public int     AircraftTypeId  { get; set; }
    public int     ManufacturerId  { get; set; }
    public string  Model           { get; set; } = string.Empty;
    public int     TotalSeats      { get; set; }
    public decimal CargoCapacityKg { get; set; }
}
