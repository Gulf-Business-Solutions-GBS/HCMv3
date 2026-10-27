using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeBasicPayDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public double Amount { get; set; }
    public PayType PayType { get; set; }
    public PayInterval PayInterval { get; set; }
    public int? EmployeeActionId { get; set; }
}

public class CreateOrEditEmployeeBasicPayDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public PayType PayType { get; set; }

    public PayInterval PayInterval { get; set; }

    public int? EmployeeActionId { get; set; }
}

public class GetEmployeeBasicPayForEditOutput
{
    public CreateOrEditEmployeeBasicPayDto EmployeeBasicPay { get; set; }
}

public class GetAllEmployeeBasicPaysInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
