using Dukan.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dukan.Web.Data.Configurations;

public sealed class FirebaseConfigConfiguration : IEntityTypeConfiguration<FirebaseConfig>
{
    public void Configure(EntityTypeBuilder<FirebaseConfig> builder)
    {
        builder.ToTable("FirebaseConfigs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProjectId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.CredentialJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(x => x.ClientEmail)
            .HasMaxLength(256);

        builder.HasOne(x => x.Subscription)
            .WithOne(s => s.FirebaseConfig)
            .HasForeignKey<FirebaseConfig>(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.SubscriptionId)
            .IsUnique();

        builder.HasIndex(x => x.ProjectId)
            .IsUnique();
    }
}
