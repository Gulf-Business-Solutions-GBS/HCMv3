using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeLoanRecoveryDto : EntityDto<int>
{
    public int EmployeeLoanId { get; set; }
    public DateTime RecoveryDate { get; set; }
    public double RecoveryAmount { get; set; }
    public LoanRecoveryMethod LoanRecoveryMethod { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeLoanRecoveryDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeLoanId { get; set; }

    [Required]
    public DateTime RecoveryDate { get; set; }

    [Required]
    public double RecoveryAmount { get; set; }

    public LoanRecoveryMethod LoanRecoveryMethod { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

public class GetEmployeeLoanRecoveryForEditOutput
{
    public CreateOrEditEmployeeLoanRecoveryDto EmployeeLoanRecovery { get; set; }
}

public class GetAllEmployeeLoanRecoveriesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeLoanId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "RecoveryDate DESC";
        }
    }
}
