namespace HRAttendance.Api.Models;

public class PermissionRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly From { get; set; }
    public TimeOnly To { get; set; }
    public string Reason { get; set; } = string.Empty;
}
