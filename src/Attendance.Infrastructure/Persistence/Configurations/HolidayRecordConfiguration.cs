using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="HolidayRecord"/> entity.
/// Maps to the <c>HolidayRecords</c> table — a global cache table, unrelated to any employee.
/// </summary>
public sealed class HolidayRecordConfiguration : IEntityTypeConfiguration<HolidayRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HolidayRecord> builder)
    {
        builder.ToTable("HolidayRecords");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .UseIdentityColumn();

        builder.Property(h => h.Date)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(h => h.HebrewName)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnType("nvarchar(200)");

        builder.Property(h => h.Category)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("nvarchar(50)");

        // Supports year-range existence checks and per-date lookups.
        builder.HasIndex(h => h.Date)
            .HasDatabaseName("IX_HolidayRecords_Date");
    }
}
