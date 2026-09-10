using HRAttendance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HRAttendance.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<PermissionRequest> PermissionRequests => Set<PermissionRequest>();
    public DbSet<AppNotification> Notifications => Set<AppNotification>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

    // --- Case-statistics domain ---
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<EmployeeReport> EmployeeReports => Set<EmployeeReport>();
    public DbSet<PreviousYearStatistics> PreviousYearStatistics => Set<PreviousYearStatistics>();
    public DbSet<CurrentYearStatistics> CurrentYearStatistics => Set<CurrentYearStatistics>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AttendanceRecord>()
            .HasIndex(a => new { a.EmployeeId, a.Date })
            .IsUnique();

        modelBuilder.Entity<AttendanceRecord>()
            .Property(a => a.Status)
            .HasConversion<string>();

        modelBuilder.Entity<AppNotification>()
            .Property(n => n.Severity)
            .HasConversion<string>();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<UserPermission>()
            .HasKey(up => new { up.UserId, up.PermissionId });

        modelBuilder.Entity<UserPermission>()
            .HasOne(up => up.User)
            .WithMany(u => u.UserPermissions)
            .HasForeignKey(up => up.UserId);

        modelBuilder.Entity<UserPermission>()
            .HasOne(up => up.Permission)
            .WithMany(p => p.UserPermissions)
            .HasForeignKey(up => up.PermissionId);

        // --- Case-statistics configuration ---

        modelBuilder.Entity<Employee>()
            .Property(e => e.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Employee>().HasIndex(e => e.NameArabic);
        modelBuilder.Entity<Employee>().HasIndex(e => e.NameEnglish);

        // Prevent duplicate monthly reports for the same year/month/office (spec section 18).
        modelBuilder.Entity<Report>()
            .HasIndex(r => new { r.Year, r.Month, r.OfficeNameArabic })
            .IsUnique();
        modelBuilder.Entity<Report>().HasIndex(r => new { r.Year, r.Month });

        modelBuilder.Entity<Report>()
            .HasOne(r => r.FinalizedByUser)
            .WithMany()
            .HasForeignKey(r => r.FinalizedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // One EmployeeReport per (Report, Employee) - no duplicates (spec section 7 & 18).
        modelBuilder.Entity<EmployeeReport>()
            .HasIndex(er => new { er.ReportId, er.EmployeeId })
            .IsUnique();

        modelBuilder.Entity<EmployeeReport>()
            .HasOne(er => er.Report)
            .WithMany(r => r.EmployeeReports)
            .HasForeignKey(er => er.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EmployeeReport>()
            .HasOne(er => er.Employee)
            .WithMany(e => e.EmployeeReports)
            .HasForeignKey(er => er.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PreviousYearStatistics>()
            .HasOne(p => p.EmployeeReport)
            .WithOne(er => er.PreviousYearStatistics)
            .HasForeignKey<PreviousYearStatistics>(p => p.EmployeeReportId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PreviousYearStatistics>()
            .HasIndex(p => p.EmployeeReportId).IsUnique();

        modelBuilder.Entity<CurrentYearStatistics>()
            .HasOne(c => c.EmployeeReport)
            .WithOne(er => er.CurrentYearStatistics)
            .HasForeignKey<CurrentYearStatistics>(c => c.EmployeeReportId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CurrentYearStatistics>()
            .HasIndex(c => c.EmployeeReportId).IsUnique();

        modelBuilder.Entity<AuditLog>().HasIndex(a => a.Timestamp);
        modelBuilder.Entity<AuditLog>().HasIndex(a => new { a.Entity, a.EntityId });
    }
}
