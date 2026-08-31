namespace HRAttendance.Api.Dtos;

public class DashboardStatsDto
{
    public DateOnly Date { get; set; }
    public int TotalEmployees { get; set; }
    public int PresentToday { get; set; }
    public int LateToday { get; set; }
    public int OnMission { get; set; }
    public int OnLeave { get; set; }
    public int AbsentToday { get; set; }
}

public class NotificationDto
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "info"; // info | warning | danger
}
