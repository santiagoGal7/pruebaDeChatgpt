namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageAllowance.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class BaggageAllowanceEntityConfiguration : IEntityTypeConfiguration<BaggageAllowanceEntity>
{
    public void Configure(EntityTypeBuilder<BaggageAllowanceEntity> builder)
    {
        builder.ToTable("baggage_allowance");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("baggage_allowance_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.CabinClassId)
               .HasColumnName("cabin_class_id")
               .IsRequired();

        builder.Property(e => e.FareTypeId)
               .HasColumnName("fare_type_id")
               .IsRequired();

        // UNIQUE (cabin_class_id, fare_type_id) — espejo de uq_ba
        builder.HasIndex(e => new { e.CabinClassId, e.FareTypeId })
               .IsUnique()
               .HasDatabaseName("uq_ba");

        builder.Property(e => e.CarryOnPieces)
               .HasColumnName("carry_on_pieces")
               .IsRequired()
               .HasDefaultValue(1);

        builder.Property(e => e.CarryOnKg)
               .HasColumnName("carry_on_kg")
               .IsRequired()
               .HasColumnType("decimal(5,2)")
               .HasDefaultValue(10m);

        builder.Property(e => e.CheckedPieces)
               .HasColumnName("checked_pieces")
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(e => e.CheckedKg)
               .HasColumnName("checked_kg")
               .IsRequired()
               .HasColumnType("decimal(5,2)")
               .HasDefaultValue(0m);
    }
}
