using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee rejoin record -- DELIBERATELY MINIMAL "record-keeping only" port of legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeRejoin</c> (Wave B). Along with
/// <see cref="EmployeeSeparation"/>, this is the biggest simplification in Wave B.
///
/// SIGNIFICANT SIMPLIFICATION vs legacy business logic: this entity is a simple audit/record-keeping
/// table this wave, NOT a working orchestration. The legacy "carry forward" boolean flags
/// (<c>RejoinWithPrevLeaveAccrual</c>/<c>RejoinWithPrevTicketAccrual</c>/
/// <c>RejoinWithPrevIndemnityAccrual</c>/<c>RejoinWithPrevLoan</c>) are kept here purely as record-keeping
/// fields -- none of their side effects (creation/adjustment of <c>EmployeeAbsAdjustment</c>,
/// <c>EmployeeGratuity</c>, <c>EmployeeTktAdjustment</c>, <c>EmployeeLoan</c>/<c>EmployeeLoanRecovery</c>,
/// or <c>TimeTransfer</c> records) are implemented, since none of those balance-adjustment entities exist
/// in this port yet.
///
/// DEFERRED / DROPPED (not ported):
/// - <c>CreateNewEmployeeCode</c> branching logic (create-new-employee-record vs reactivate-existing):
///   this entity just records which <see cref="Employee"/> the rejoin applies to; no new Employee row is
///   auto-created.
/// - <c>ValidatePreviousEntry</c>/chain-blocking against Exit/FnFPayment/SalaryRegister/other-Rejoin state.
/// - <c>SaveEmployeeInformation</c>/<c>UpdatePreviousValues</c> orchestration (Employee.Status flips,
///   quota/loan carry-forward creation and rollback).
/// </summary>
public class EmployeeRejoin : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime RejoinDate { get; set; }

    [StringLength(2000)]
    public virtual string Comments { get; set; }

    public virtual bool CarryForwardLeaveBalance { get; set; }

    public virtual bool CarryForwardTicketBalance { get; set; }

    public virtual bool CarryForwardIndemnityBalance { get; set; }

    public virtual bool CarryForwardLoanBalance { get; set; }
}
