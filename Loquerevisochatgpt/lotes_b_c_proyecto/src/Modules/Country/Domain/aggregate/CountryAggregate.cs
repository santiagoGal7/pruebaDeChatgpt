namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.Aggregate;

using Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Domain.ValueObject;

public sealed class CountryAggregate
{
    public CountryId Id   { get; private set; }
    public string    Name { get; private set; }

    private CountryAggregate()
    {
        Id   = null!;
        Name = null!;
    }

    public CountryAggregate(CountryId id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Country name cannot be empty.", nameof(name));

        if (name.Trim().Length > 100)
            throw new ArgumentException("Country name cannot exceed 100 characters.", nameof(name));

        Id   = id;
        Name = name.Trim();
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Country name cannot be empty.", nameof(newName));

        if (newName.Trim().Length > 100)
            throw new ArgumentException("Country name cannot exceed 100 characters.", nameof(newName));

        Name = newName.Trim();
    }
}
