using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeRelationshipDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int RelationshipTypeId { get; set; }
    public string RelationshipTypeName { get; set; }
    public int RelatedEmployeeId { get; set; }
    public string RelatedEmployeeCode { get; set; }
    public string RelatedEmployeeFullNameEn { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class CreateOrEditEmployeeRelationshipDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int RelationshipTypeId { get; set; }

    [Required]
    public int RelatedEmployeeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}

public class GetEmployeeRelationshipForEditOutput
{
    public CreateOrEditEmployeeRelationshipDto EmployeeRelationship { get; set; }
}

public class GetAllEmployeeRelationshipsInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
