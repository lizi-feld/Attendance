using Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Persistence;

/// <summary>
/// The EF Core database context for the Time Attendance System.
/// Applies all entity configurations from the Infrastructure assembly automatically.
/// </summary>
public sealed class AttendanceDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="AttendanceDbContext"/>
    /// with the specified options.
    /// </summary>
    /// <param name="options">
    /// The DbContext options that include the SQL Server connection string
    /// configured in the DI registration.
    /// </param>
    public AttendanceDbContext(DbContextOptions<AttendanceDbContext> options)
        : base(options) { }

    /// <summary>Gets the <see cref="Employee"/> table set.</summary>
    public DbSet<Employee> Employees => Set<Employee>();

    /// <summary>Gets the <see cref="AttendanceRecord"/> table set.</summary>
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AttendanceDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
