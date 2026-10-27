using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.SuccessionManagement.Dto;

public class EmpPotentialAssessmentDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Description { get; set; }
    public double Score { get; set; }
    public int RatingId { get; set; }
    public string RatingNameEn { get; set; }
    public ScoreSource Source { get; set; }
    public int NineBoxRatingId { get; set; }
    public string NineBoxRatingNameEn { get; set; }
}

public class CreateOrEditEmpPotentialAssessmentDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [StringLength(2000)]
    public string Description { get; set; }

    public double Score { get; set; }

    [Required]
    public int RatingId { get; set; }

    public ScoreSource Source { get; set; }

    [Required]
    public int NineBoxRatingId { get; set; }
}

public class GetEmpPotentialAssessmentForEditOutput
{
    public CreateOrEditEmpPotentialAssessmentDto EmpPotentialAssessment { get; set; }
}

public class GetAllEmpPotentialAssessmentsInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
