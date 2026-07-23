using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeRejoinDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public DateTime RejoinDate { get; set; }
    public string Comments { get; set; }
    public bool CarryForwardLeaveBalance { get; set; }
    public bool CarryForwardTicketBalance { get; set; }
    public bool CarryForwardIndemnityBalance { get; set; }
    public bool CarryForwardLoanBalance { get; set; }
}

public class CreateOrEditEmployeeRejoinDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime RejoinDate { get; set; }

    [StringLength(2000)]
    public string Comments { get; set; }

    public bool CarryForwardLeaveBalance { get; set; }

    public bool CarryForwardTicketBalance { get; set; }

    public bool CarryForwardIndemnityBalance { get; set; }

    public bool CarryForwardLoanBalance { get; set; }
}

public class GetEmployeeRejoinForEditOutput
{
    public CreateOrEditEmployeeRejoinDto EmployeeRejoin { get; set; }
}

public class GetAllEmployeeRejoinsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? EmployeeId { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "RejoinDate DESC";
        }
    }
}
