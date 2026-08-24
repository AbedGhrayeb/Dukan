using Dukan.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dukan.Web.Data.Configurations;

public sealed class ContactSettingsConfiguration : IEntityTypeConfiguration<ContactSettings>
{
    public void Configure(EntityTypeBuilder<ContactSettings> builder)
    {
        builder.ToTable("ContactSettings");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.PhoneNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.WhatsAppNumber)
            .HasMaxLength(30)
            .IsRequired();
    }
}
