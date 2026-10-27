using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeHRServiceDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int HRServiceTypeId { get; set; }
    public string HRServiceTypeNameEn { get; set; }
    public string HRServiceTypeNameAr { get; set; }
    public DateTime ServiceDate { get; set; }
    public double Amount { get; set; }
    public string Comments { get; set; }

    /// <summary>Null when the row was recorded directly rather than routed, so the screen can show it as
    /// simply recorded instead of implying it went through approval.</summary>
    public RequestStatus? WorkflowStatus { get; set; }

    public int? WorkflowRequestId { get; set; }
}

public class CreateOrEditEmployeeHRServiceDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int HRServiceTypeId { get; set; }

    [Required]
    public DateTime ServiceDate { get; set; }

    // A finite upper bound: double.MaxValue serialises into the OpenAPI document as 1.797E+308, which
    // NSwag cannot parse, and proxy generation then fails for the whole API.
    [Range(0, 1_000_000_000)]
    public double Amount { get; set; }

    [StringLength(1000)]
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

public class GetEmployeeHRServiceForEditOutput
{
    public CreateOrEditEmployeeHRServiceDto EmployeeHRService { get; set; }
}

public class GetAllEmployeeHRServicesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public int? HRServiceTypeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ServiceDate DESC";
        }
    }
}
