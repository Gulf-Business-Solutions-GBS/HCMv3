using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeAccountAssignmentDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? CostElement1Id { get; set; }
    public string CostElement1NameEn { get; set; }
    public int? CostElement2Id { get; set; }
    public string CostElement2NameEn { get; set; }
    public int? CostElement3Id { get; set; }
    public string CostElement3NameEn { get; set; }
    public int? CostElement4Id { get; set; }
    public string CostElement4NameEn { get; set; }
    public int? CostElement5Id { get; set; }
    public string CostElement5NameEn { get; set; }
    public int? CostElement6Id { get; set; }
    public string CostElement6NameEn { get; set; }
    public int? CostElement7Id { get; set; }
    public string CostElement7NameEn { get; set; }
    public double CostPercentage { get; set; }
}

public class CreateOrEditEmployeeAccountAssignmentDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public int? CostElement1Id { get; set; }
    public int? CostElement2Id { get; set; }
    public int? CostElement3Id { get; set; }
    public int? CostElement4Id { get; set; }
    public int? CostElement5Id { get; set; }
    public int? CostElement6Id { get; set; }
    public int? CostElement7Id { get; set; }

    public double CostPercentage { get; set; }
}

public class GetEmployeeAccountAssignmentForEditOutput
{
    public CreateOrEditEmployeeAccountAssignmentDto EmployeeAccountAssignment { get; set; }
}

public class GetAllEmployeeAccountAssignmentsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "StartDate DESC";
        }
    }
}
