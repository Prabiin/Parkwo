using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingApp.Domain;

namespace ParkingApp.Infrastructure.Persistence.Configurations;

public class SavedParkingConfiguration : IEntityTypeConfiguration<SavedParking>
{
    public void Configure(EntityTypeBuilder<SavedParking> builder)
    {
        builder.ToTable("SavedParkings", t => t.HasCheckConstraint(
            "CK_SavedParkings_ExactlyOneTarget",
            "(\"FacilityId\" IS NOT NULL)::int + (\"FreeParkingFacilityId\" IS NOT NULL)::int = 1"));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Facility)
            .WithMany()
            .HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.FreeParkingFacility)
            .WithMany()
            .HasForeignKey(x => x.FreeParkingFacilityId)
            .OnDelete(DeleteBehavior.Cascade);

        // One bookmark per rider per place. Postgres treats NULLs as distinct,
        // so each unique index only bites for its own kind of target.
        builder.HasIndex(x => new { x.UserId, x.FacilityId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.FreeParkingFacilityId }).IsUnique();
    }
}
