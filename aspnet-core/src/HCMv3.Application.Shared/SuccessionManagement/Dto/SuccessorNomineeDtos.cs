using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.SuccessionManagement.Dto;

public class SuccessorNomineeDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int NomineeId { get; set; }
    public string NomineeCode { get; set; }
    public string NomineeFullNameEn { get; set; }
    public int ReadinessId { get; set; }
    public string ReadinessNameEn { get; set; }
    public int RankingId { get; set; }
    public string RankingNameEn { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditSuccessorNomineeDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int NomineeId { get; set; }

    [Required]
    public int ReadinessId { get; set; }

    [Required]
    public int RankingId { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

public class GetSuccessorNomineeForEditOutput
{
    public CreateOrEditSuccessorNomineeDto SuccessorNominee { get; set; }
}

public class GetAllSuccessorNomineesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "Id DESC";
        }
    }
}
