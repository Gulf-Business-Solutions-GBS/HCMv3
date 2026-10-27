using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;

namespace HCMv3.GoLiveSetup.Dto;

/// <summary>The month and payroll area whose payroll results to show -- the same two things Run payroll was given.</summary>
public class GoLivePayrollResultsInput
{
    [Required]
    public DateTime? PayDate { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string PayrollAreaName { get; set; }
}

/// <summary>Step 3: what the month's payroll paid, one line per employee in the payroll area.</summary>
public class GoLivePayrollResultsOutput
{
    /// <summary>The month end the results are for.</summary>
    public DateTime PayInDate { get; set; }

    /// <summary>For the Payroll Sheet report, which filters by payroll area id. Null when the area does not exist.</summary>
    public int? PayrollAreaId { get; set; }

    public List<GoLivePayrollResultRowDto> Employees { get; set; } = new();

    public double TotalEarnings { get; set; }

    public double TotalDeductions { get; set; }

    public double TotalNetPay { get; set; }

    /// <summary>Employees with a salary register for the month.</summary>
    public int EmployeesPaid { get; set; }
}

public class GoLivePayrollResultRowDto
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    /// <summary>False when the payroll did not reach this employee for the month.</summary>
    public bool HasRegister { get; set; }

    /// <summary>Earning wage types, Net Pay itself left out.</summary>
    public double Earnings { get; set; }

    public double Deductions { get; set; }

    /// <summary>Wage type 9000.</summary>
    public double NetPay { get; set; }
}

public class GoLiveSalaryRegisterInput
{
    [Required]
    public DateTime? PayDate { get; set; }

    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }
}

/// <summary>One employee's Salary Register for the month: every wage type the engine wrote.</summary>
public class GoLiveSalaryRegisterDto
{
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }

    /// <summary>The month the register pays for; equal to <see cref="InPeriod"/> for the month's own register.</summary>
    public DateTime ForPeriod { get; set; }

    /// <summary>The month it was paid in.</summary>
    public DateTime InPeriod { get; set; }

    /// <summary>Empty when there is no register -- the payroll has not reached this employee for the month.</summary>
    public List<GoLiveSalaryRegisterLineDto> Lines { get; set; } = new();

    /// <summary>Day-by-period breakdown behind a line, where the engine split one across periods.</summary>
    public List<GoLiveSalaryRegisterDaysDto> Days { get; set; } = new();
}

public class GoLiveSalaryRegisterLineDto
{
    public string WageCode { get; set; }

    public string WageName { get; set; }

    /// <summary>Earning, deduction, provision, or a time type (day counter).</summary>
    public BillingType Group { get; set; }

    public double Rate { get; set; }

    public double Number { get; set; }

    public double Amount { get; set; }
}

public class GoLiveSalaryRegisterDaysDto
{
    public string WageCode { get; set; }

    public string WageName { get; set; }

    public DateTime Period { get; set; }

    public double Rate { get; set; }

    public double Number { get; set; }

    public double Amount { get; set; }
}
