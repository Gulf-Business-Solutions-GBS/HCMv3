using System;
using Abp.Application.Services.Dto;

namespace HCMv3.SystemSecurity.Dto;

public class RetroReasonLogDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeFullName { get; set; }
    public DateTime ChangedOn { get; set; }
    public DateTime RetroDate { get; set; }
    public string EntityObjectName { get; set; }
    public int EntityObjectId { get; set; }
    public string RetroReason { get; set; }
}
