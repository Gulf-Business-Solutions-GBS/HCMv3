using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class LoanInstallmentDto : EntityDto<int>
{
    public int EmployeeLoanId { get; set; }
    public DateTime InstallmentDate { get; set; }
    public decimal InstallmentAmount { get; set; }
    public InstallmentStatus InstallmentStatus { get; set; }
}

public class CreateOrEditLoanInstallmentDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeLoanId { get; set; }

    [Required]
    public DateTime InstallmentDate { get; set; }

    public decimal InstallmentAmount { get; set; }

    public InstallmentStatus InstallmentStatus { get; set; }
}

public class GetLoanInstallmentForEditOutput
{
    public CreateOrEditLoanInstallmentDto LoanInstallment { get; set; }
}

public class GetAllLoanInstallmentsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeLoanId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "InstallmentDate DESC";
        }
    }
}
