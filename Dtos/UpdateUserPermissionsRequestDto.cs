namespace HRAttendance.Api.Dtos
{
    public class UpdateUserPermissionsRequestDto
    {
        public int UserId { get; set; }

        public List<int> PermissionIds { get; set; } = [];
    }
}