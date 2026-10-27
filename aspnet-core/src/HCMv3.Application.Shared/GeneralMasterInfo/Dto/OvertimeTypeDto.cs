using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="OvertimeType"/>.
/// </summary>
public class OvertimeTypeDto : EntityDto<int>
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public double OvertimeRate { get; set; }

    /// <summary>
    /// Legacy <c>OvertimeType.WageType</c> FK (restored wave P8). Both the Project Cost Engine and the
    /// Payroll Engine identify overtime wage types through it, so it must be settable from this master.
    /// </summary>
    public int? WageTypeId { get; set; }

    public string WageTypeCode { get; set; }

    public string WageTypeNameEn { get; set; }
}
