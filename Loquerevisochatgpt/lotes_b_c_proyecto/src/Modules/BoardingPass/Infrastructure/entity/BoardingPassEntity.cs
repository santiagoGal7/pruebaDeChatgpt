namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BoardingPass.Infrastructure.Entity;

public sealed class BoardingPassEntity
{
    public int     Id            { get; set; }
    public int     CheckInId     { get; set; }
    public int?    GateId        { get; set; }
    public string? BoardingGroup { get; set; }
    public int     FlightSeatId  { get; set; }
}
