using Abp.Application.Services.Dto;

namespace HCMv3.SystemSecurity.Dto;

public class EmployeesNotificationDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int? TaskEventId { get; set; }
    public string TaskEventSubject { get; set; }
    public bool IsNotificationDeleted { get; set; }
    public bool IsNotificationRead { get; set; }
}
