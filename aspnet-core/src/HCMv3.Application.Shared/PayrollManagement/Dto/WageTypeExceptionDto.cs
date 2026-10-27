using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="WageTypeException"/>.
/// </summary>
public class WageTypeExceptionDto : EntityDto<int>
{
    public int WageTypeId { get; set; }

    public string WageTypeName { get; set; }

    public int ExceptWageTypeId { get; set; }

    public string ExceptWageTypeName { get; set; }
}
