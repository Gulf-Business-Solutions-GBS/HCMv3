using System;
using Abp.Application.Services.Dto;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="SalaryRegister"/>.
/// </summary>
public class SalaryRegisterDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public DateTime ForPeriod { get; set; }

    public DateTime InPeriod { get; set; }

    public PayrollRunType PayrollRunType { get; set; }

    public PayrollResultType PayrollResultType { get; set; }

    public int? PayrollReasonTypeId { get; set; }

    public string PayrollReasonTypeName { get; set; }

    public DateTime OffCyclePaymentDate { get; set; }

    public bool Locked { get; set; }

    /// <summary>
    /// Legacy <c>Employee.CurrentPayrollArea</c>: the payroll area of the employee's org assignment in force
    /// today, not the one the register was calculated under.
    /// </summary>
    public int? PayrollAreaId { get; set; }

    public string PayrollAreaName { get; set; }
}
