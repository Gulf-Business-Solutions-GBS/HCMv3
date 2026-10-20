using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// One effective-dated instalment amount on an employee loan (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeLoanInstallment</c>, added 16/09/2026).
///
/// <para>
/// The amount applies from <see cref="StartDate"/> (legacy label "Effective Date", always the first of a month)
/// until <see cref="EndDate"/>, which is never typed: it is the day before the next row's start, or 31/12/9999 for
/// the last row, and is rewritten on every save and delete -- see <see cref="Services.LoanInstallmentSchedule"/>. Payroll
/// reads the row in force for the month and falls back to <see cref="EmployeeLoan.FixedAmount"/> only when the
/// loan has none, which is what keeps loans recorded before the schedule existed recovering as they did.
/// </para>
///
/// <para>
/// Not to be confused with <see cref="LoanInstallment"/>, a different and older legacy table.
/// </para>
/// </summary>
public class EmployeeLoanInstallment : FullAuditedEntity, IMustHaveTenant
{
    public const int MaxRemarkLength = 500;

    public int TenantId { get; set; }

    public virtual int EmployeeLoanId { get; set; }

    public virtual EmployeeLoan EmployeeLoan { get; set; }

    /// <summary>Legacy display name "Effective Date".</summary>
    public virtual DateTime StartDate { get; set; }

    /// <summary>Maintained automatically; never shown on the form, as in legacy.</summary>
    public virtual DateTime EndDate { get; set; }

    public virtual double Amount { get; set; }

    [StringLength(MaxRemarkLength)]
    public virtual string Remark { get; set; }
}
