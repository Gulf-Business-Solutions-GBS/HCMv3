using System;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace HCMv3.PersonnelAdministration.Dto;

public class EmployeeAbsenceDto : EntityDto<int>
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; }
    public string EmployeeFullNameEn { get; set; }
    public int AbsenceTypeId { get; set; }
    public string AbsenceTypeName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public HalfdayType HalfdayType { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double AbsenceHours { get; set; }
    public bool IsTravelling { get; set; }
    public double CalendarDays { get; set; }
    public double TotalLeaveDays { get; set; }
    public LeavePaymentType LeavePaymentType { get; set; }
    public DateTime PaymentDate { get; set; }
    public bool AdvanceLeavePaid { get; set; }
    public string Comments { get; set; }
    public DateTime ReturnDate { get; set; }
    public bool IsReturned { get; set; }
    public DateTime InitialEndDate { get; set; }
    public int? ExtAbsenceTypeId { get; set; }
    public string ExtAbsenceTypeName { get; set; }
    public DateTime? ExtStartDate { get; set; }
    public DateTime? ExtEndDate { get; set; }
    public double? ExtTotalLeaveDays { get; set; }
}

public class CreateOrEditEmployeeAbsenceDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int AbsenceTypeId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public HalfdayType HalfdayType { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool IsTravelling { get; set; }

    public LeavePaymentType LeavePaymentType { get; set; }

    public DateTime PaymentDate { get; set; }

    public bool AdvanceLeavePaid { get; set; }

    [StringLength(4000)]
    public string Comments { get; set; }

    public DateTime ReturnDate { get; set; }

    public bool IsReturned { get; set; }

    public int? ExtAbsenceTypeId { get; set; }

    public DateTime? ExtStartDate { get; set; }

    public DateTime? ExtEndDate { get; set; }
}

public class GetEmployeeAbsenceForEditOutput
{
    public CreateOrEditEmployeeAbsenceDto EmployeeAbsence { get; set; }
}

public class GetAllEmployeeAbsencesInput : PagedAndSortedResultRequestDto, IShouldNormalize
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
