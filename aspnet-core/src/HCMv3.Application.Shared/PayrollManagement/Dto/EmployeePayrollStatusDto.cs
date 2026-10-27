using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="EmployeePayrollStatus"/>.
/// </summary>
public class EmployeePayrollStatusDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; }

    public DateTime EarliestRetroAccountingDate { get; set; }

    public DateTime AccountedTo { get; set; }

    public DateTime RunPayrollUpto { get; set; }

    public DateTime EarliestMasterDataChange { get; set; }

    public DateTime DoNoAccountAfter { get; set; }

    public bool PersonLocked { get; set; }

    public bool PayrollCorrection { get; set; }

    public DateTime EarliestRecalculationDate { get; set; }

    public DateTime PersonalCalendarFrom { get; set; }

    public DateTime NextTimeEvaluationFrom { get; set; }
}
