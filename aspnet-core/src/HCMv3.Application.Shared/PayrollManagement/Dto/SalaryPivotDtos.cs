using System;
using System.Collections.Generic;

namespace HCMv3.PayrollManagement.Dto;

/// <summary>
/// Filter for the salary pivot dataset, ported from legacy
/// <c>HCM10.Web/UserControls/uscPivotChart.ascx.cs</c> (line 166).
/// </summary>
public class GetSalaryPivotDataInput
{
    /// <summary>
    /// THIS PORT'S ADDITION. Legacy bound the pivot grid to the ENTIRE
    /// <c>SalaryRegisterDetail</c> table with no period filter whatsoever, which is unbounded and would
    /// time out over HTTP on any real dataset. Both bounds are optional, so passing neither reproduces
    /// legacy exactly.
    /// </summary>
    public DateTime? FromPeriod { get; set; }

    /// <summary>See <see cref="FromPeriod"/>.</summary>
    public DateTime? ToPeriod { get; set; }
}

/// <summary>
/// One flattened salary line -- exactly legacy's anonymous projection (line 166):
/// Employee, ForPeriod, Amount, Code, WageType, Department, BillingType.
/// </summary>
public class SalaryPivotRowDto
{
    /// <summary>Legacy <c>Employee</c> = <c>SalaryRegister.Employee.FullNameEn</c>.</summary>
    public string Employee { get; set; }

    /// <summary>Legacy <c>Code</c> = <c>SalaryRegister.Employee.Code</c>.</summary>
    public string Code { get; set; }

    /// <summary>Legacy <c>ForPeriod</c>.</summary>
    public DateTime ForPeriod { get; set; }

    /// <summary>Legacy <c>WageType</c> = <c>WageType.NameEn</c>.</summary>
    public string WageType { get; set; }

    /// <summary>
    /// Legacy <c>Department</c> = <c>Employee.OrgAssignment[0].EmployeeGroup.NameEn</c> -- note it is the
    /// employee GROUP, not the org unit, despite the column name. Preserved.
    /// </summary>
    public string Department { get; set; }

    /// <summary>Legacy <c>BillingType</c> = <c>WageType.BillingType</c>.</summary>
    public BillingType BillingType { get; set; }

    /// <summary>Legacy <c>Amount</c>.</summary>
    public double Amount { get; set; }
}

/// <summary>Output of <c>GetSalaryPivotData</c>.</summary>
public class SalaryPivotDataDto
{
    public List<SalaryPivotRowDto> Rows { get; set; } = new List<SalaryPivotRowDto>();
}
