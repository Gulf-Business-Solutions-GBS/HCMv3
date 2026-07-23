using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeeSubGroupAbsenceType"/>.
/// </summary>
public class EmployeeSubGroupAbsenceTypeDto : EntityDto<int>
{
    public int EmployeeSubGroupId { get; set; }

    public string EmployeeSubGroupName { get; set; }

    public int AbsenceTypeId { get; set; }

    public string AbsenceTypeName { get; set; }
}
