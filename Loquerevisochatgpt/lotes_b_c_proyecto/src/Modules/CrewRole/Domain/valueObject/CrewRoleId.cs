namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.CrewRole.Domain.ValueObject;

public readonly record struct CrewRoleId(int Value)
{
    public static CrewRoleId New(int value)
    {
        if (value <= 0)
            throw new ArgumentException("CrewRoleId must be a positive integer.", nameof(value));
        return new CrewRoleId(value);
    }
    public override string ToString() => Value.ToString();
}
