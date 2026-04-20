namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Currency.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class CurrencyEntityConfiguration : IEntityTypeConfiguration<CurrencyEntity>
{
    public void Configure(EntityTypeBuilder<CurrencyEntity> builder)
    {
        builder.ToTable("currency");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("currency_id")
               .ValueGeneratedOnAdd();

        // CHAR(3) — código ISO 4217
        builder.Property(e => e.IsoCode)
               .HasColumnName("iso_code")
               .IsRequired()
               .HasMaxLength(3)
               .IsFixedLength();

        builder.HasIndex(e => e.IsoCode)
               .IsUnique()
               .HasDatabaseName("uq_currency_iso_code");

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(80);

        builder.HasIndex(e => e.Name)
               .IsUnique()
               .HasDatabaseName("uq_currency_name");

        builder.Property(e => e.Symbol)
               .HasColumnName("symbol")
               .IsRequired()
               .HasMaxLength(5);
    }
}
