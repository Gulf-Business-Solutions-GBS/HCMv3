using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Per-employee payroll status/lock tracking (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.EmpPayrollStatus</c>; note the legacy file name
/// mismatch with its class name -- the actual class inside is <c>EmployeePayrollStatus</c>, and a separate
/// legacy <c>EmployeePayrollStatus.cs</c> file is a confirmed dead/orphaned duplicate excluded from the
/// legacy .csproj, so this port intentionally follows the class name, not the "Emp..." file name). Exactly
/// one row is allowed per <see cref="Employee"/> (enforced only on create in
/// <c>EmployeePayrollStatusesAppService</c>, legacy "Employee can have only single Employee PayrollStatus"
/// parity).
/// </summary>
public class EmployeePayrollStatus : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime EarliestRetroAccountingDate { get; set; }

    public virtual DateTime AccountedTo { get; set; }

    public virtual DateTime RunPayrollUpto { get; set; }

    public virtual DateTime EarliestMasterDataChange { get; set; }

    public virtual DateTime DoNoAccountAfter { get; set; }

    public virtual bool PersonLocked { get; set; }

    public virtual bool PayrollCorrection { get; set; }

    public virtual DateTime EarliestRecalculationDate { get; set; }

    public virtual DateTime PersonalCalendarFrom { get; set; }

    public virtual DateTime NextTimeEvaluationFrom { get; set; }
}
