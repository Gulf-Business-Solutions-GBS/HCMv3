using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeOvertimeDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime EffectiveDate { get; set; }
    public int OvertimeTypeId { get; set; }
    public string OvertimeTypeNameEn { get; set; }
    public double OvertimeHours { get; set; }
    public double ActualOvertimeHours { get; set; }
    public double ApprovedOvertimeHours { get; set; }
    public string Comments { get; set; }

    /// <summary>
    /// Read-only. What the approved hours are worth, as legacy showed it (legacy non-persistent
    /// <c>EmployeeOvertime.ExpectedOvertimeAmount</c>). Calculated on read, never stored.
    /// </summary>
    public double ExpectedOvertimeAmount { get; set; }
}

public class CreateOrEditEmployeeOvertimeDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime EffectiveDate { get; set; }

    [Required]
    public int OvertimeTypeId { get; set; }

    public double OvertimeHours { get; set; }

    public double ActualOvertimeHours { get; set; }

    public double ApprovedOvertimeHours { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    /// <summary>
    /// Record this as final instead of sending it for approval.
    ///
    /// <para>
    /// Offered only to a user holding <c>RecordWithoutApproval</c>, and only on a type whose catalog row
    /// permits a bypass. The server checks both and refuses otherwise, so this being true is a request
    /// rather than a decision.
    /// </para>
    /// </summary>
    public bool RecordDirectly { get; set; }
}

public class GetEmployeeOvertimeForEditOutput
{
    public CreateOrEditEmployeeOvertimeDto EmployeeOvertime { get; set; }

    /// <summary>
    /// Read-only, for display in the edit form. Kept out of <see cref="CreateOrEditEmployeeOvertimeDto"/> so
    /// it is never posted back. See <see cref="EmployeeOvertimeDto.ExpectedOvertimeAmount"/>.
    /// </summary>
    public double ExpectedOvertimeAmount { get; set; }
}

public class GetAllEmployeeOvertimesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "EffectiveDate DESC";
        }
    }
}
