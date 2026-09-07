using System.Collections.Generic;
using Abp.Dependency;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Services;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration.BusinessRules;

/// <summary>
/// Exposes leave (<see cref="EmployeeAbsence"/>) to the rule engine, with the applying employee and the
/// leave type alongside it.
///
/// <para>
/// Three parameters because leave eligibility is almost never a statement about the leave row alone. "A male
/// employee cannot take maternity leave" needs the employee's gender and the type's identity, neither of
/// which is a column on <c>EmployeeAbsence</c>. Resolving both into the context once, in the hook, is what
/// keeps a rule author from having to think about joins -- and what keeps the cost of asking bounded.
/// </para>
///
/// <para>
/// The leave row itself is WRITABLE so derivation rules can compute day counts or default a return date.
/// The employee and the leave type are read-only: a leave rule has no business editing the person or
/// redefining the type.
/// </para>
/// </summary>
public class EmployeeAbsenceRuleBaseObjectProvider : IRuleBaseObjectProvider, ISingletonDependency
{
    /// <summary>The base object code stored in <c>BusinessRule.BaseObject</c>. Never rename.</summary>
    public const string BaseObjectCode = "EmployeeAbsence";

    public const string LeaveParameter = "Leave";
    public const string EmployeeParameter = "Employee";
    public const string LeaveTypeParameter = "LeaveType";

    public IEnumerable<RuleBaseObjectDescriptor> GetBaseObjects()
    {
        yield return RuleBaseObjectBuilder<EmployeeAbsence>
            .For(BaseObjectCode, "Leave")
            .SupportsEvents(
                RuleEventType.OnInit,
                RuleEventType.BeforeSave,
                RuleEventType.OnValidate)

            .Parameter<EmployeeAbsence>(LeaveParameter, "Leave", isWritable: true, p => p
                .Read("Id", "Id", RuleDataType.Integer, a => (long)a.Id)
                .Read("EmployeeId", "Employee", RuleDataType.Integer, a => (long)a.EmployeeId)
                .Read("AbsenceTypeId", "AbsenceType", RuleDataType.Lookup, a => (long)a.AbsenceTypeId, "AbsenceType")
                .ReadWrite("StartDate", "StartDate", RuleDataType.Date, a => a.StartDate, (a, v) => a.StartDate = v)
                .ReadWrite("EndDate", "EndDate", RuleDataType.Date, a => a.EndDate, (a, v) => a.EndDate = v)
                .ReadWrite("TotalLeaveDays", "TotalLeaveDays", RuleDataType.Decimal, a => (decimal)a.TotalLeaveDays, (a, v) => a.TotalLeaveDays = (double)v)
                .ReadWrite("CalendarDays", "CalendarDays", RuleDataType.Decimal, a => (decimal)a.CalendarDays, (a, v) => a.CalendarDays = (double)v)
                .ReadWrite("ReturnDate", "ReturnDate", RuleDataType.Date, a => a.ReturnDate, (a, v) => a.ReturnDate = v)
                .ReadWrite("AdvanceLeavePaid", "AdvanceLeavePaid", RuleDataType.Boolean, a => a.AdvanceLeavePaid, (a, v) => a.AdvanceLeavePaid = v)
                .Read("LeavePaymentType", "LeavePaymentType", RuleDataType.Enum, a => (long)a.LeavePaymentType, nameof(LeavePaymentType))
                .Read("IsTravelling", "IsTravelling", RuleDataType.Boolean, a => a.IsTravelling))

            .Parameter<RuleEmployeeSnapshot>(EmployeeParameter, "Employee", isWritable: false, p => p
                .Read("EmployeeId", "Employee", RuleDataType.Integer, e => (long)e.EmployeeId)
                .Read("Code", "Code", RuleDataType.String, e => e.Code)
                .Read("Gender", "Gender", RuleDataType.Enum, e => (long)e.Gender, nameof(Gender))
                .Read("JoiningDate", "JoiningDate", RuleDataType.Date, e => e.JoiningDate)
                .Read("Status", "Status", RuleDataType.Enum, e => (long)e.Status, nameof(EmployeeStatus))
                .Read("JobTitleId", "JobTitle", RuleDataType.Lookup, e => e.JobTitleId, "JobTitle")
                .Read("JobTitleCode", "JobTitleCode", RuleDataType.String, e => e.JobTitleCode)
                .Read("EmployeeGroupId", "EmployeeGroup", RuleDataType.Lookup, e => e.EmployeeGroupId, "EmployeeGroup")
                .Read("OrganizationUnitId", "OrganizationUnit", RuleDataType.Integer, e => e.OrganizationUnitId)
                .Read("IsManager", "IsManager", RuleDataType.Boolean, e => e.IsManager)
                .Read("DirectReportCount", "DirectReportCount", RuleDataType.Integer, e => (long)e.DirectReportCount))

            .Parameter<AbsenceType>(LeaveTypeParameter, "AbsenceType", isWritable: false, p => p
                .Read("Id", "Id", RuleDataType.Integer, t => (long)t.Id)
                .Read("Code", "Code", RuleDataType.String, t => t.Code)
                .Read("NameEn", "NameEn", RuleDataType.String, t => t.NameEn)
                .Read("IsPaid", "IsPaid", RuleDataType.Boolean, t => t.IsPaid)
                .Read("IsActive", "Active", RuleDataType.Boolean, t => t.IsActive)
                .Read("MinActiveDaysInService", "MinActiveDaysInService", RuleDataType.Decimal, t => (decimal)t.MinActiveDaysInService))

            .Build();
    }
}
