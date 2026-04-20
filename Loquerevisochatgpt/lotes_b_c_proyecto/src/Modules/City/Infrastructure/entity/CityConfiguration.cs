namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.City.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Configuración EF Core para <see cref="CityEntity"/>.
/// snake_case se aplica automáticamente; solo se configura
/// lo que la convención no puede inferir.
/// </summary>
public sealed class CityConfiguration : IEntityTypeConfiguration<CityEntity>
{
    public void Configure(EntityTypeBuilder<CityEntity> builder)
    {
        builder.ToTable("city");

        builder.HasKey(e => e.CityId);

        builder.Property(e => e.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.CreatedAt)
               .IsRequired()
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Índice único compuesto (name, country_id)
        builder.HasIndex(e => new { e.Name, e.CountryId })
               .IsUnique()
               .HasDatabaseName("uq_city");
    }
}
