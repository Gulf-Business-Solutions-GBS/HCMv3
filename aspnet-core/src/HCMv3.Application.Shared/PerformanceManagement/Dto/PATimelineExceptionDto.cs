using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PerformanceManagement.Dto;

public class PATimelineExceptionDto : EntityDto<int>
{
    public int PAPeriodId { get; set; }

    public string PAPeriodName { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public ActivityType ActivityType { get; set; }

    public DateTime ExtensionDate { get; set; }
}
