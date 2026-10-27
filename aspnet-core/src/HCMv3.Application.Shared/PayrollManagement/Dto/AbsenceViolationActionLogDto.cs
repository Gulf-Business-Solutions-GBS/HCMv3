using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="AbsenceViolationActionLog"/>.
/// </summary>
public class AbsenceViolationActionLogDto : EntityDto<int>
{
    public int AbsenceViolationActionRecordId { get; set; }

    public int Step { get; set; }

    public AbsenceViolationAction Action { get; set; }

    public DateTime ActionDate { get; set; }

    public int ObjectOId { get; set; }
}
