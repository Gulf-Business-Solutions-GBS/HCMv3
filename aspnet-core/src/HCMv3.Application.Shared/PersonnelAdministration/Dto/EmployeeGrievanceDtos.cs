using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeGrievanceDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int GrievanceTypeId { get; set; }
    public string GrievanceTypeNameEn { get; set; }
    public DateTime GrievanceDate { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeGrievanceDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int GrievanceTypeId { get; set; }

    [Required]
    public DateTime GrievanceDate { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

public class GetEmployeeGrievanceForEditOutput
{
    public CreateOrEditEmployeeGrievanceDto EmployeeGrievance { get; set; }
}

public class GetAllEmployeeGrievancesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "GrievanceDate DESC";
        }
    }
}
