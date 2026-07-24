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
}

public class GetEmployeeOvertimeForEditOutput
{
    public CreateOrEditEmployeeOvertimeDto EmployeeOvertime { get; set; }
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
