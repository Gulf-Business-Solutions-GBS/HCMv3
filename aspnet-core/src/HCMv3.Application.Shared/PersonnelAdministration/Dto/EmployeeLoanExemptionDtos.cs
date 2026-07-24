using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeLoanExemptionDto : EntityDto<int>
{
    public int EmployeeLoanId { get; set; }
    public DateTime ExemptionDate { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeLoanExemptionDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeLoanId { get; set; }

    [Required]
    public DateTime ExemptionDate { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

public class GetEmployeeLoanExemptionForEditOutput
{
    public CreateOrEditEmployeeLoanExemptionDto EmployeeLoanExemption { get; set; }
}

public class GetAllEmployeeLoanExemptionsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeLoanId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "ExemptionDate DESC";
        }
    }
}
