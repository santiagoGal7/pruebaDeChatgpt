namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CheckIn.Infrastructure.Entity;

public sealed class CheckInEntity
{
    public int      Id              { get; set; }
    public int      TicketId        { get; set; }
    public DateTime CheckInTime     { get; set; }
    public int      CheckInStatusId { get; set; }
    public string?  CounterNumber   { get; set; }
}
