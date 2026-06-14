using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="AttendanceRecord"/> entity.
/// Maps to the <c>AttendanceRecords</c> table and configures column constraints,
/// query-supporting indexes, and the many-to-one relationship with <see cref="Employee"/>.
/// </summary>
public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .UseIdentityColumn();

        builder.Property(a => a.EmployeeId)
            .IsRequired();

        // All timestamps store Europe/Zurich time as sourced from the external time provider.
        builder.Property(a => a.ClockInTime)
            .IsRequired()
            .HasColumnType("datetime2");

        // Nullable — null indicates an active (not yet closed) session.
        builder.Property(a => a.ClockOutTime)
            .IsRequired(false)
            .HasColumnType("datetime2");

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2");

        // Nullable — only populated on manual/retroactive adjustments; empty for regular clock-in/out.
        builder.Property(a => a.Note)
            .IsRequired(false)
            .HasMaxLength(500)
            .HasColumnType("nvarchar(500)");

        // Computed properties — not mapped to columns.
        builder.Ignore(a => a.IsActive);
        builder.Ignore(a => a.Duration);

        // Supports GetByEmployeeIdAsync and GetActiveRecordAsync.
        builder.HasIndex(a => a.EmployeeId)
            .HasDatabaseName("IX_AttendanceRecords_EmployeeId");

        // Supports GetByDateAsync and dashboard date-range queries.
        builder.HasIndex(a => a.ClockInTime)
            .HasDatabaseName("IX_AttendanceRecords_ClockInTime");

        // Composite index: covers the WHERE EmployeeId = x AND ClockOutTime IS NULL
        // pattern used by GetActiveRecordAsync — the most frequent attendance lookup.
        builder.HasIndex(a => new { a.EmployeeId, a.ClockOutTime })
            .HasDatabaseName("IX_AttendanceRecords_EmployeeId_ClockOutTime");

        // Relationship is owned here (FK side).
        // Navigation backing field on Employee is configured in EmployeeConfiguration.
        builder.HasOne(a => a.Employee)
            .WithMany(e => e.AttendanceRecords)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
