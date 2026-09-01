using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Dto;

/// <summary>
/// Admin-side view of an employment letter request. Carries the workflow status because HR needs to see
/// whether a letter has been signed off before they issue it -- the same fact the employee sees as
/// "CanPrint" on their own screen.
/// </summary>
public class EmployeeLetterRequestDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public int DocumentTypeId { get; set; }

    public string DocumentTypeNameEn { get; set; }

    public string DocumentTypeNameAr { get; set; }

    public DateTime EffectiveDate { get; set; }

    public string AddressToEn { get; set; }

    public string AddressToAr { get; set; }

    public string Comments { get; set; }

    public RequestStatus? WorkflowStatus { get; set; }

    public int? WorkflowRequestId { get; set; }

    /// <summary>Approved and nothing else -- the same rule the ESS screen applies. Stated here too so the
    /// admin grid and the employee's list cannot disagree about whether a letter is issuable.</summary>
    public bool CanPrint { get; set; }
}

public class CreateOrEditEmployeeLetterRequestDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int DocumentTypeId { get; set; }

    [Required]
    public DateTime EffectiveDate { get; set; }

    [Required]
    [StringLength(EmployeeLetterRequestConsts.MaxTextLength)]
    public string AddressToEn { get; set; }

    [Required]
    [StringLength(EmployeeLetterRequestConsts.MaxTextLength)]
    public string AddressToAr { get; set; }

    [StringLength(EmployeeLetterRequestConsts.MaxTextLength)]
    public string Comments { get; set; }
}

public class GetEmployeeLetterRequestForEditOutput
{
    public CreateOrEditEmployeeLetterRequestDto EmployeeLetterRequest { get; set; }
}

public class GetAllEmployeeLetterRequestsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public int? DocumentTypeId { get; set; }

    /// <summary>
    /// Include rows still in flight. Off by default so the grid shows the settled record, matching every
    /// other admin grid; on, it disables the ApprovedOnly filter so HR can see what is pending on them.
    /// </summary>
    public bool IncludePending { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "EffectiveDate DESC";
        }
    }
}
