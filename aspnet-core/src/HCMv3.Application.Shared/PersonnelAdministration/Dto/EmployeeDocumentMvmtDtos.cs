using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeDocumentMvmtDto : EntityDto<int>
{
    public int EmployeeDocumentId { get; set; }
    public string EmployeeDocumentNum { get; set; }
    public DateTime WithdrawDate { get; set; }
    public DateTime ReturnDate { get; set; }
    public DateTime ActualReturnDate { get; set; }
    public string Comments { get; set; }
}

public class CreateOrEditEmployeeDocumentMvmtDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeDocumentId { get; set; }

    [Required]
    public DateTime WithdrawDate { get; set; }

    [Required]
    public DateTime ReturnDate { get; set; }

    public DateTime? ActualReturnDate { get; set; }

    [StringLength(500)]
    public string Comments { get; set; }
}

public class GetEmployeeDocumentMvmtForEditOutput
{
    public CreateOrEditEmployeeDocumentMvmtDto EmployeeDocumentMvmt { get; set; }
}

public class GetAllEmployeeDocumentMvmtsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeDocumentId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "WithdrawDate DESC";
        }
    }
}
