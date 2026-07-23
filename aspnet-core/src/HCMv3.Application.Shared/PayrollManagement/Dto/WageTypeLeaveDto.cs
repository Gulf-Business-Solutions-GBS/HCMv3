using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="WageTypeLeave"/>.
/// </summary>
public class WageTypeLeaveDto : EntityDto<int>
{
    public int WageTypeId { get; set; }

    public string WageTypeName { get; set; }

    public int AbsenceTypeId { get; set; }

    public string AbsenceTypeName { get; set; }
}
