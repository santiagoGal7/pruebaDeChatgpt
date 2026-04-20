namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Country.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class CountryEntityConfiguration : IEntityTypeConfiguration<CountryEntity>
{
    public void Configure(EntityTypeBuilder<CountryEntity> builder)
    {
        builder.ToTable("country");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("country_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
               .HasColumnName("name")
               .IsRequired()
               .HasMaxLength(100);

        builder.HasIndex(e => e.Name)
               .IsUnique()
               .HasDatabaseName("uq_country_name");
    }
}
