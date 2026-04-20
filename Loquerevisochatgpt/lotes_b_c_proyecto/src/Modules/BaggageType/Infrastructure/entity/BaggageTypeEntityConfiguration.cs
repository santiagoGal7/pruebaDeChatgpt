namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.BaggageType.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class BaggageTypeEntityConfiguration : IEntityTypeConfiguration<BaggageTypeEntity>
{
    public void Configure(EntityTypeBuilder<BaggageTypeEntity> builder)
    {
        builder.ToTable("baggage_type");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("baggage_type_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(80);

        builder.HasIndex(e => e.Name)
               .IsUnique()
               .HasDatabaseName("uq_baggage_type_name");

        builder.Property(e => e.MaxWeightKg)
               .HasColumnName("max_weight_kg")
               .IsRequired()
               .HasColumnType("decimal(5,2)");

        builder.Property(e => e.ExtraFee)
               .HasColumnName("extra_fee")
               .IsRequired()
               .HasColumnType("decimal(10,2)")
               .HasDefaultValue(0m);
    }
}
