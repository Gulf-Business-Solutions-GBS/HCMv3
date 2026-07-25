using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.SuccessionManagement.Dto;

public class EmpTalentMatrixDataDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int PerformanceRatingId { get; set; }
    public string PerformanceRatingNameEn { get; set; }
    public int PotentialRatingId { get; set; }
    public string PotentialRatingNameEn { get; set; }
    public int OverallRatingId { get; set; }
    public string OverallRatingNameEn { get; set; }
    public int CalibratedRatingId { get; set; }
    public string CalibratedRatingNameEn { get; set; }
}

public class CreateOrEditEmpTalentMatrixDataDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public int PerformanceRatingId { get; set; }

    [Required]
    public int PotentialRatingId { get; set; }

    [Required]
    public int OverallRatingId { get; set; }

    [Required]
    public int CalibratedRatingId { get; set; }
}

public class GetEmpTalentMatrixDataForEditOutput
{
    public CreateOrEditEmpTalentMatrixDataDto EmpTalentMatrixData { get; set; }
}

public class GetAllEmpTalentMatrixDatasInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
