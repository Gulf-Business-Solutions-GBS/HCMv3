using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Filter for the time log approval grid (ported from legacy
/// <c>HCM10.ESS/Repository/TimeLogRepository.GetTimeRegisterData</c>, lines 30-100).
///
/// <para>
/// Legacy took an org unit TYPE code as well as an org unit, then branched seven ways across
/// <c>OrgLevel1..OrgLevel7</c> on <c>EmployeeOrgAssignment</c>. This port has one
/// <c>EmployeeOrgAssignment.OrganizationUnitId</c> pointing into ABP's own org unit tree, so the seven
/// branches collapse into one filter and the type code is not needed.
/// </para>
/// </summary>
public class GetTimeLogApprovalGridInput
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    /// <summary>One employee, or null to take everybody in <see cref="OrganizationUnitId"/>.</summary>
    public int? EmployeeId { get; set; }

    /// <summary>The department to review. Ignored when <see cref="EmployeeId"/> is given, as in legacy.</summary>
    public long? OrganizationUnitId { get; set; }
}
