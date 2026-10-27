using System;

namespace HCMv3.GeneralMasterInfo.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="OvertimeTypeDetail"/>. <c>Id == null</c> means create; otherwise update.
/// </summary>
public class CreateOrEditOvertimeTypeDetailDto
{
    public int? Id { get; set; }

    public int? OvertimeTypeId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public OvertimeRateType OvertimeRateType { get; set; }

    public double Rate { get; set; }
}
