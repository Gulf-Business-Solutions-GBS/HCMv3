namespace HCMv3.LegacyMigrator.Plan;

/// <summary>
/// The dependency-ordered migration plan.
///
/// <para>ORDER IS NOT GUESSED. It was derived by taking the set of entities the ported Time Engine and its
/// dependencies actually read (the <c>IRepository&lt;T&gt;</c> constructor injections across
/// <c>TimeEngineService</c>, <c>TimeRegisterFieldCalculator</c>, <c>TimeDataService</c>,
/// <c>PunchResolutionStrategy*</c>), computing the transitive foreign-key closure of the corresponding tables
/// in the HCMv3 database, then topologically levelling that closure by FK dependency. The levels below are the
/// output of that levelling; the closure contains no cycles.</para>
///
/// <para>CALCULATED OUTPUTS ARE DELIBERATELY ABSENT. <c>TimeRegister</c>, <c>SalaryRegister</c> and
/// <c>SalaryRegisterDetail</c> are what our engines are supposed to PRODUCE. Copying legacy's values for them
/// into the target would destroy the very comparison this migration exists to enable, so they are never
/// migrated -- legacy's copies stay in the source database and are read there as the expected-result baseline.
/// Same reasoning for <c>MonthlyTimeRegister</c>, <c>YearlyTimeRegister</c> and the simulation tables.</para>
/// </summary>
public static class MigrationPlan
{
    public static IReadOnlyList<TableSpec> Build()
    {
        var plan = new List<TableSpec>();

        // ---------- LEVEL 0: root lookups with no outgoing FKs -------------------------------------
        void Lookup(string target, string source, Action<TableSpec> _ = null) =>
            plan.Add(new TableSpec { Target = target, Source = source, Scope = ScopeKind.All });

        Lookup("AccessUnits", "AccessUnit");
        Lookup("BreakShifts", "BreakShift");
        Lookup("BudgetingGroups", "BudgetingGroup");
        Lookup("ContractTypes", "ContractType");
        Lookup("CostCenterGroups", "CostCenterGroup");
        Lookup("CostElementTypes", "CostElementType");
        Lookup("Countries", "Country");
        Lookup("Currencies", "Currency");
        Lookup("DependantTypes", "DependantType");
        Lookup("EmployeeGroups", "EmployeeGroup");
        Lookup("Governorates", "Governate");   // legacy spells it "Governate"
        Lookup("HolidayCalendars", "HolidayCalendar");
        Lookup("JobTitles", "JobTitle");
        Lookup("MaritalStatuses", "MaritalStatus");
        Lookup("NationalityGroups", "NationalityGroup");
        Lookup("OvertimeTypes", "OvertimeType");
        Lookup("PayrollPeriods", "PayrollPeriod");
        Lookup("PersonalActions", "PersonalAction");
        Lookup("PostingGroups", "PostingGroup");
        Lookup("Qualifications", "Qualification");
        Lookup("RecruitmentTypes", "RecruitmentType");
        Lookup("Religions", "Religion");
        Lookup("States", "State");
        Lookup("TimePolicies", "TimePolicy");
        Lookup("TimeTransferTypes", "TimeTransferType");
        Lookup("Titles", "Title");
        Lookup("TransTypes", "TransType");
        Lookup("WageTypes", "WageType");
        Lookup("WorkLocations", "WorkLocation");
        Lookup("AbsenceDeductionRules", "AbsenceDeductionRule");
        Lookup("AbsenceViolationRules", "AbsenceViolationRule");

        // ---- CONFIGURATION SINGLETONS -----------------------------------------------------------------
        // These are NOT reachable from the FK closure -- nothing points at them -- but the engines read them
        // through ISysSettingsAccessor for ~25 behavioural flags (EnableRosterDuty, CountWeekoffOnPH,
        // CountRestDayOnPH, ForfeitRule, EarliestEvaluationDate, LeaveDeductionStartMonth...). Leaving them
        // empty does not fail; it silently substitutes C# defaults, which is exactly the kind of quiet
        // divergence that would invalidate a legacy-vs-port comparison. They are migrated explicitly.
        Lookup("ManageTimeSettingses", "ManageTimeSettings");
        Lookup("ManagePayrollSettingses", "ManagePayrollSettings");
        Lookup("ManagePersonnelSettingses", "ManagePersonnelSettings");
        Lookup("ManageSecuritySettingses", "ManageSecuritySettings");
        Lookup("ManageGeneralSettingses", "ManageGeneralSettings");
        Lookup("ManageOrgSettingses", "ManageOrgSettings");

        // SystemNotification drives the engine's AnnualLeaveMaxLimitReachedNotification flag.
        Lookup("NotificationGroups", "NotificationGroup");
        Lookup("NotificationConfigs", "NotificationConfig");
        Lookup("SystemNotifications", "SystemNotification");

        // ---------- LEVEL 1 ------------------------------------------------------------------------
        Lookup("Cities", "City");
        Lookup("CostCenters", "CostCenter");
        Lookup("CostElements", "CostElement");
        Lookup("Nationalities", "Nationality");
        Lookup("PersonalActionReasons", "PersonalActionReason");
        Lookup("AbsenceTypes", "AbsenceType");
        Lookup("TicketTypes", "TicketType");
        Lookup("Duties", "Duty");
        Lookup("EmployeeSubGroups", "EmployeeSubGroup");
        Lookup("Holidays", "Holiday");

        // AccountInfos is deliberately NOT migrated. It entered the FK closure only through the nullable
        // PayrollAreas.FIBankId, and its own AccountTypeId is a NOT NULL FK to PayrollAccountTypes -- a table
        // that has no counterpart at all in the legacy schema. Migrating AccountInfo would therefore mean
        // inventing account types. PayrollAreas.FIBankId is left NULL instead (reported per run), which no
        // part of the Time or Payroll engine reads.

        // ---------- LEVEL 2 ------------------------------------------------------------------------
        Lookup("AttendanceTypes", "AttendanceType");
        Lookup("DutyPeriods", "DutyPeriod");
        Lookup("HolidayPeriods", "HolidayPeriod");
        Lookup("PayrollAreas", "PayrollArea");
        Lookup("EmployeeSubGroupAbsences", "EmployeeSubGroupAbsence");
        Lookup("EmployeeSubGroupAbsenceTypes", "EmployeeSubGroupAbsenceType");
        Lookup("EmployeeSubGroupGratuities", "EmployeeSubGroupGratuity");
        Lookup("EmployeeSubGroupPayments", "EmployeeSubGroupPayment");
        Lookup("EmployeeSubGroupTickets", "EmployeeSubGroupTicket");
        Lookup("PayrollControlRecords", "PayrollControlRecord");

        // PayrollCalendar defines the period boundaries every payroll run is validated against. Omitting it does
        // not degrade gracefully -- PayrollEngineService aborts the whole run with "Payroll Calender does not
        // have record for Payroll month you are running", so a migrated customer cannot run payroll at all until
        // this lands. It was missed originally because the plan's FK closure was seeded from the TIME engine's
        // repository injections, and nothing in the time engine reads it.
        // Must follow PayrollAreas: PayrollCalendars.PayrollAreaId is NOT NULL and resolves through the id map.
        // Legacy's Company and Locked columns have no target counterpart and are dropped (period locking lives
        // on PayrollControlRecord.PeriodLocked in HCMv3).
        Lookup("PayrollCalendars", "PayrollCalendar");

        // ---------- LEVEL 2/3: the employees themselves ---------------------------------------------
        plan.Add(new TableSpec
        {
            Target = "Employees",
            Source = "Employee",
            Scope = ScopeKind.Employee,
            ScopeColumn = "OID",
            ColumnOverrides = { ["GovernorateId"] = "Governate" }
        });

        // ---------- LEVEL 3: per-employee transactional inputs ---------------------------------------
        void PerEmployee(string target, string source, Action<TableSpec> configure = null)
        {
            var spec = new TableSpec
            {
                Target = target,
                Source = source,
                Scope = ScopeKind.Employee,
                ScopeColumn = "Employee"
            };
            configure?.Invoke(spec);
            plan.Add(spec);
        }

        // EmployeeAction LOOKS like a lookup (EmployeeOrgAssignments.EmployeeActionId points at it) but it is
        // per-employee transactional data with a NOT NULL EmployeeId, so it must follow Employees. Sequencing
        // it with the level-2 lookups silently dropped all 1,417 rows on an unresolvable FK.
        PerEmployee("EmployeeActions", "EmployeeAction");

        PerEmployee("EmployeePayrollStatuses", "EmployeePayrollStatus");
        PerEmployee("EmployeeDutyPeriods", "EmployeeDutyPeriod");
        PerEmployee("EmployeeTimePolicies", "EmployeeTimePolicy");
        PerEmployee("EmployeeSubstituteDuties", "EmployeeSubstituteDuty");
        PerEmployee("EmployeeAbsences", "EmployeeAbsence");
        PerEmployee("EmployeeAbsenceQuotas", "EmployeeAbsenceQuota");
        PerEmployee("EmployeeAbsAdjustments", "EmployeeAbsAdjustment");
        PerEmployee("EmployeeAttendances", "EmployeeAttendance");
        PerEmployee("EmployeeAttendanceQuotas", "EmployeeAttendanceQuota");
        PerEmployee("EmployeeOvertimes", "EmployeeOvertime");
        PerEmployee("EmployeeTickets", "EmployeeTicket");
        PerEmployee("EmployeeTicketQuotas", "EmployeeTicketQuota");
        PerEmployee("EmployeeGratuityQuotas", "EmployeeGratuityQuota");
        PerEmployee("EmployeeDependants", "EmployeeDependant");
        PerEmployee("EmployeeBenefitExceptions", "EmployeeBenefitException");
        PerEmployee("EmployeeTimesheets", "EmployeeTimesheet");
        PerEmployee("TimeEvents", "TimeEvent");
        PerEmployee("TimeTransfers", "TimeTransfer");

        // ---------- LEVEL 4 --------------------------------------------------------------------------
        PerEmployee("EmployeeContracts", "EmployeeContract");
        PerEmployee("EmployeeBasicPays", "EmployeeBasicPay");
        PerEmployee("EmployeeOrgAssignments", "EmployeeOrgAssignment");

        plan.Add(new TableSpec
        {
            Target = "EmployeeDependantExceptions",
            Source = "EmployeeDependantException",
            Scope = ScopeKind.Parent,
            ScopeColumn = "EmployeeDependant",
            ScopeParentEntity = "EmployeeDependants"
        });

        // ---------- LEVEL 5 --------------------------------------------------------------------------
        plan.Add(new TableSpec
        {
            Target = "EmployeeBasicPayDetails",
            Source = "EmployeeBasicPayDetail",
            Scope = ScopeKind.Parent,
            ScopeColumn = "EmployeeBasicPay",
            ScopeParentEntity = "EmployeeBasicPays"
        });

        return plan;
    }
}
