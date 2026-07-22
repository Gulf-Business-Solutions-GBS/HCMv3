using System;
using Abp.Application.Services.Dto;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Read/list DTO for <see cref="OvertimeTypeDetail"/>.
/// </summary>
public class OvertimeTypeDetailDto : EntityDto<int>
{
    public int? OvertimeTypeId { get; set; }

    public string OvertimeTypeName { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public OvertimeRateType OvertimeRateType { get; set; }

    public double Rate { get; set; }
}
