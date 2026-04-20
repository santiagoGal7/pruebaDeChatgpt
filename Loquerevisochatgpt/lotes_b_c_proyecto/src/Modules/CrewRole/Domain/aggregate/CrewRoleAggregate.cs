namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Domain.ValueObject;

/// <summary>
/// Rol operativo de un tripulante en un vuelo concreto.
/// Distinto de job_position (cargo laboral estructural).
/// Catálogo: CAPTAIN, FIRST_OFFICER, FLIGHT_ATTENDANT, PURSER.
/// </summary>
public sealed class CrewRoleAggregate
{
    public CrewRoleId Id   { get; private set; }
    public string     Name { get; private set; } = string.Empty;

    private CrewRoleAggregate() { }

    public static CrewRoleAggregate Create(string name)
    {
        ValidateName(name);
        return new CrewRoleAggregate { Name = name.Trim().ToUpperInvariant() };
    }

    public static CrewRoleAggregate Reconstitute(int id, string name) =>
        new() { Id = CrewRoleId.New(id), Name = name };

    public void Update(string name)
    {
        ValidateName(name);
        Name = name.Trim().ToUpperInvariant();
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Crew role name cannot be empty.", nameof(name));
        if (name.Length > 50)
            throw new ArgumentException("Crew role name cannot exceed 50 characters.", nameof(name));
    }
}
