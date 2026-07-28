using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Attendance.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="AbsenceRecord"/> entity.
/// Maps to the <c>AbsenceRecords</c> table and configures column constraints,
/// query-supporting indexes, and the many-to-one relationship with <see cref="Employee"/>.
/// </summary>
public sealed class AbsenceRecordConfiguration : IEntityTypeConfiguration<AbsenceRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AbsenceRecord> builder)
    {
        builder.ToTable("AbsenceRecords");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .UseIdentityColumn();

        builder.Property(a => a.EmployeeId)
            .IsRequired();

        builder.Property(a => a.Date)
            .IsRequired()
            .HasColumnType("date");

        // Stored as int — enum numeric values are stable by design (see AbsenceType).
        builder.Property(a => a.Type)
            .IsRequired()
            .HasConversion<int>();

        // Nullable — only populated for absence types that require a supporting document.
        builder.Property(a => a.DocumentUrl)
            .IsRequired(false)
            .HasMaxLength(1000)
            .HasColumnType("nvarchar(1000)");

        builder.Property(a => a.Note)
            .IsRequired(false)
            .HasMaxLength(500)
            .HasColumnType("nvarchar(500)");

        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2");

        // Supports "all absences for employee X" queries.
        builder.HasIndex(a => a.EmployeeId)
            .HasDatabaseName("IX_AbsenceRecords_EmployeeId");

        // Composite index: covers "did employee X report an absence on date Y" lookups.
        builder.HasIndex(a => new { a.EmployeeId, a.Date })
            .HasDatabaseName("IX_AbsenceRecords_EmployeeId_Date");

        // One-directional relationship — Employee has no reverse AbsenceRecords collection,
        // mirroring how RefreshToken relates to Employee in this model.
        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
