using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="AbsenceViolationActionRecord"/>.
/// </summary>
public class AbsenceViolationActionRecordDto : EntityDto<int>
{
    public ActionLogType ActionLogType { get; set; }

    public LogStatus LogStatus { get; set; }

    public int EntityObjectId { get; set; }
}
