using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeDisciplinaryDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public long? OrganizationUnitId { get; set; }
    public int? WorkLocationId { get; set; }
    public string WorkLocationNameEn { get; set; }
    public DateTime DisciplinaryDate { get; set; }
    public DateTime DisciplinaryTime { get; set; }
    public int DisciplinaryTypeId { get; set; }
    public string DisciplinaryTypeNameEn { get; set; }
    public int? ReportedById { get; set; }
    public string ReportedByFullNameEn { get; set; }
    public bool WLSendToEmp { get; set; }
    public bool WLRecvFromEmp { get; set; }
    public string Comments { get; set; }
    public Severity Severity { get; set; }
    public int? DisciplinaryDeductionTypeId { get; set; }
    public string DisciplinaryDeductionTypeNameEn { get; set; }

    /// <summary>
    /// <c>null</c> for a record that was never routed for approval -- which is every record while the
    /// transaction type is dormant, and what keeps this list behaving exactly as it did before.
    /// </summary>
    public RequestStatus? WorkflowStatus { get; set; }

    public int? WorkflowRequestId { get; set; }
}

/// <summary>
/// READ-ONLY view of the escalation ladder for one employee / offence type / date. Nothing here is stored;
/// it is all computed by <c>IDisciplinaryEscalationService</c> at read time.
///
/// <para>
/// It exists so the person raising a disciplinary record sees the CONSEQUENCE before they submit it. The
/// penalty for the same offence differs depending on how many of them the employee already has inside the
/// repeat window, and someone filing a third late arrival should know they are about to trigger a
/// deduction rather than discover it after an approver has agreed to it.
/// </para>
/// </summary>
public class DisciplinaryEscalationDto
{
    public int EmployeeId { get; set; }

    public int DisciplinaryTypeId { get; set; }

    public string DisciplinaryTypeNameEn { get; set; }

    public string DisciplinaryTypeNameAr { get; set; }

    /// <summary>How far back this offence type looks when counting repeats.</summary>
    public int RepeatDurationInDays { get; set; }

    public DateTime WindowStartDate { get; set; }

    public DateTime WindowEndDate { get; set; }

    /// <summary>Prior offences of this type inside the window, excluding the record being assessed.</summary>
    public int PriorDisciplinaryCount { get; set; }

    /// <summary>Which offence this one is. Starts at 1.</summary>
    public int OffenceNumber { get; set; }

    /// <summary>The penalty the ladder resolves to; <c>null</c> when the type has no tier this low.</summary>
    public int? DisciplinaryDeductionTypeId { get; set; }

    public string DisciplinaryDeductionTypeNameEn { get; set; }

    public string DisciplinaryDeductionTypeNameAr { get; set; }

    /// <summary>Earlier records of this type for this employee, most recent first.</summary>
    public List<DisciplinaryHistoryItemDto> History { get; set; } = new List<DisciplinaryHistoryItemDto>();
}

public class DisciplinaryHistoryItemDto : EntityDto<int>
{
    public DateTime DisciplinaryDate { get; set; }

    public Severity Severity { get; set; }

    public int? DisciplinaryDeductionTypeId { get; set; }

    public string DisciplinaryDeductionTypeNameEn { get; set; }

    /// <summary>Whether it counted towards <see cref="DisciplinaryEscalationDto.PriorDisciplinaryCount"/>.</summary>
    public bool IsWithinRepeatWindow { get; set; }

    public RequestStatus? WorkflowStatus { get; set; }
}

/// <summary>
/// Asks what a disciplinary record WOULD cost, for a record that may not exist yet -- which is the case
/// that matters, since the point is to see the penalty before submitting.
/// </summary>
public class GetDisciplinaryEscalationInput
{
    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int DisciplinaryTypeId { get; set; }

    [Required]
    public DateTime DisciplinaryDate { get; set; }

    /// <summary>The record being edited, so it does not count itself. Omitted for a new one.</summary>
    public int? ExcludeDisciplinaryId { get; set; }
}

public class CreateOrEditEmployeeDisciplinaryDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    public long? OrganizationUnitId { get; set; }

    public int? WorkLocationId { get; set; }

    [Required]
    public DateTime DisciplinaryDate { get; set; }

    [Required]
    public DateTime DisciplinaryTime { get; set; }

    [Required]
    public int DisciplinaryTypeId { get; set; }

    public int? ReportedById { get; set; }

    public bool WLSendToEmp { get; set; }

    public bool WLRecvFromEmp { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }

    public Severity Severity { get; set; }

    public int? DisciplinaryDeductionTypeId { get; set; }
}

public class GetEmployeeDisciplinaryForEditOutput
{
    public CreateOrEditEmployeeDisciplinaryDto EmployeeDisciplinary { get; set; }

    /// <summary>
    /// Where this record sits on its type's ladder, recomputed on every read rather than stored. Read-only:
    /// nothing on it is sent back, and the penalty that finally lands on the row is written by the workflow
    /// handler on approval, not by whoever edits the form.
    /// </summary>
    public DisciplinaryEscalationDto Escalation { get; set; }
}

public class GetAllEmployeeDisciplinariesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "DisciplinaryDate DESC";
        }
    }
}
