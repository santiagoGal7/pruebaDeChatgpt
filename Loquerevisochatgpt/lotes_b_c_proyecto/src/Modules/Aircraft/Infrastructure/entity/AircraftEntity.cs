namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Aircraft.Infrastructure.Entity;

public sealed class AircraftEntity
{
    public int       AircraftId          { get; set; }
    public int       AirlineId           { get; set; }
    public int       AircraftTypeId      { get; set; }
    public string    RegistrationNumber  { get; set; } = string.Empty;
    public int       ManufactureYear     { get; set; }
    public bool      IsActive            { get; set; }
    public DateTime  CreatedAt           { get; set; }
    public DateTime? UpdatedAt           { get; set; }
}
