using System;
using Abp.Application.Services.Dto;

namespace HCMv3.ManpowerPlanningManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="PositionRelationship"/>.
/// </summary>
public class PositionRelationshipDto : EntityDto<int>
{
    public int PositionId { get; set; }

    public string PositionCodeWithNameEn { get; set; }

    public PositionRelationType RelationType { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
