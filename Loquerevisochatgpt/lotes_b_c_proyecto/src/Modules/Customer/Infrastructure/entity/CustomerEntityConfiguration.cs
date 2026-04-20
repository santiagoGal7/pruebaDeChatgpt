namespace Sistema_de_gestion_de_tiquetes_Aereos.Modules.Customer.Infrastructure.Entity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class CustomerEntityConfiguration : IEntityTypeConfiguration<CustomerEntity>
{
    public void Configure(EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("customer");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("customer_id")
               .ValueGeneratedOnAdd();

        builder.Property(e => e.PersonId)
               .HasColumnName("person_id")
               .IsRequired();

        // person_id UNIQUE — una persona solo puede ser cliente una vez.
        builder.HasIndex(e => e.PersonId)
               .IsUnique()
               .HasDatabaseName("uq_customer_person_id");

        builder.Property(e => e.Phone)
               .HasColumnName("phone")
               .IsRequired(false)
               .HasMaxLength(30);

        builder.Property(e => e.Email)
               .HasColumnName("email")
               .IsRequired(false)
               .HasMaxLength(120);

        // UNIQUE (email) con filtro para no violar unicidad entre NULLs.
        // MySQL: múltiples NULL en columna UNIQUE son permitidos, EF aplica filtro.
        builder.HasIndex(e => e.Email)
               .IsUnique()
               .HasFilter("email IS NOT NULL")
               .HasDatabaseName("uq_customer_email");

        builder.Property(e => e.CreatedAt)
               .HasColumnName("created_at")
               .IsRequired()
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt)
               .HasColumnName("updated_at")
               .IsRequired(false);
    }
}
