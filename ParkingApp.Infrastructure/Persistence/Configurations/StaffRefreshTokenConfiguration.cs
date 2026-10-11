using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class StaffRefreshTokenConfiguration : IEntityTypeConfiguration<StaffRefreshToken>
{
    public void Configure(EntityTypeBuilder<StaffRefreshToken> builder)
    {
        builder.ToTable("StaffRefreshTokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        builder.Property(x => x.RevokedAtUtc);

        builder.Property(x => x.ReplacedByToken)
            .HasMaxLength(512);

        builder.Property(x => x.RevokedReason)
            .HasMaxLength(256);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc);

        builder.HasOne(x => x.Staff)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.StaffId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Token)
            .IsUnique();

        builder.HasIndex(x => x.StaffId);
    }
}
