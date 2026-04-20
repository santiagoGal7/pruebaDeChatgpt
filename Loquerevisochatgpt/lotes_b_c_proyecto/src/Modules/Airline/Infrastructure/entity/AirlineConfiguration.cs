namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Airline.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class AirlineConfiguration : IEntityTypeConfiguration<AirlineEntity>
{
    public void Configure(EntityTypeBuilder<AirlineEntity> builder)
    {
        builder.ToTable("airline");
        builder.HasKey(e => e.AirlineId);

        builder.Property(e => e.IataCode).IsRequired().HasMaxLength(2).IsFixedLength();
        builder.HasIndex(e => e.IataCode).IsUnique();

        builder.Property(e => e.Name).IsRequired().HasMaxLength(120);
        builder.HasIndex(e => e.Name).IsUnique();

        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.UpdatedAt).IsRequired(false);
    }
}
