using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="AbsenceTypeException"/>.
/// </summary>
public class AbsenceTypeExceptionDto : EntityDto<int>
{
    public int AbsenceTypeId { get; set; }

    public string AbsenceTypeName { get; set; }

    public int ExceptAbsenceTypeId { get; set; }

    public string ExceptAbsenceTypeName { get; set; }
}
