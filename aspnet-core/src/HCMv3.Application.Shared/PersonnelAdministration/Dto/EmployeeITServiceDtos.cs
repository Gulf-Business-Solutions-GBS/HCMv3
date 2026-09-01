using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeITServiceDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int ITServiceTypeId { get; set; }
    public string ITServiceTypeNameEn { get; set; }
    public string ITServiceTypeNameAr { get; set; }
    public DateTime ServiceDate { get; set; }
    public string Comments { get; set; }

    /// <summary>Null when the row was recorded directly rather than routed.</summary>
    public RequestStatus? WorkflowStatus { get; set; }

    public int? WorkflowRequestId { get; set; }
}

public class CreateOrEditEmployeeITServiceDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int ITServiceTypeId { get; set; }

    [Required]
    public DateTime ServiceDate { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

public class GetEmployeeITServiceForEditOutput
{
    public CreateOrEditEmployeeITServiceDto EmployeeITService { get; set; }
}

public class GetAllEmployeeITServicesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public int? ITServiceTypeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ServiceDate DESC";
        }
    }
}
