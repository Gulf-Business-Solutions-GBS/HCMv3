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
        // `configure` is honoured (it used to be an ignored placeholder) so a lookup can declare
        // TableSpec.LogicalForeignKeys -- the FK columns that carry no FK CONSTRAINT and are consequently
        // invisible to ColumnMapper's sys.foreign_keys lookup. See TableSpec.LogicalForeignKeys.
        void Lookup(string target, string source, Action<TableSpec> configure = null)
        {
            var spec = new TableSpec { Target = target, Source = source, Scope = ScopeKind.All };
            configure?.Invoke(spec);
            plan.Add(spec);
        }

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
        // OvertimeTypes is NOT here -- it has an FK to WageTypes and must follow it. See below.
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

        // MUST follow WageTypes. OvertimeTypes.WageTypeId is a NULLABLE FK into WageTypes, and the migrator
        // resolves FKs through LegacyIdMap -- so if it runs first, the map holds no WageTypes rows yet, the FK
        // silently resolves to NULL, and the row still inserts. Nothing fails; the damage only shows up much
        // later as MISSING PAY.
        //
        // That is exactly what happened: legacy maps OvertimeType 1->1007, 2->1008, 3->1009, 4->1042, but all
        // four landed with WageTypeId NULL. The overtime pay service then had no wage type to post against and
        // produced nothing, so wage codes 1007/1008/1009 were absent from every June 2024 register despite the
        // employees having 306 overtime records between them.
        //
        // Same failure mode the EmployeeAction comment below describes (it dropped 1,417 rows on an
        // unresolvable FK) -- but worse, because a NULLABLE FK does not even drop the row. This is the only
        // level-0 lookup with an FK to another migrated table; verified against sys.foreign_keys.
        Lookup("OvertimeTypes", "OvertimeType");

        // MUST follow WageTypes, for exactly the reason spelled out for OvertimeTypes above.
        // LoanTypes.WageTypeId (restored wave P9) is a NULLABLE FK into WageTypes and is the ONLY route by
        // which wage code 2001 "Loan Recovery Deduction" is dispatched -- legacy has "2001" commented out of
        // its wage-code list (legacy Program.cs line 2691) and builds membership from
        // LoanType.WageType.Oid instead (line 2700). Sequenced before WageTypes, all four legacy loan types
        // (all of which point at wage type OID 25 = code 2001) would land with WageTypeId NULL, no row would
        // fail, and the deduction would simply never be posted.
        Lookup("LoanTypes", "LoanType");

        Lookup("WorkLocations", "WorkLocation");
        Lookup("AbsenceDeductionRules", "AbsenceDeductionRule");
        Lookup("AbsenceViolationRules", "AbsenceViolationRule");

        // ---- CONFIGURATION SINGLETONS -----------------------------------------------------------------
        // These are NOT reachable from the FK closure -- nothing points at them -- but the engines read them
        // through ISysSettingsAccessor for ~25 behavioural flags (EnableRosterDuty, CountWeekoffOnPH,
        // CountRestDayOnPH, ForfeitRule, EarliestEvaluationDate, LeaveDeductionStartMonth...). Leaving them
        // empty does not fail; it silently substitutes C# defaults, which is exactly the kind of quiet
        // divergence that would invalidate a legacy-vs-port comparison. They are migrated explicitly.
        // ManageTimeSettingses.AbsenceViolationRuleId: same unconstrained-FK defect (AbsenceViolationRules is
        // declared above, at level 0). NULL in legacy today, so latent rather than damaged -- verified.
        Lookup("ManageTimeSettingses", "ManageTimeSettings",
            s => s.LogicalForeignKeys["AbsenceViolationRuleId"] = "AbsenceViolationRules");
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

        // Root lookups with no outgoing FKs of their own, but pulled up to LEVEL 1 because the social
        // security allowance tables below reference them with NOT NULL FKs -- so if these are missing, every
        // allowance row is DROPPED outright rather than merely nulled. Both are genuinely required, not
        // theoretical: all 11 Education rows and all 12 EducationBranch rows are referenced.
        Lookup("Educations", "Education");
        Lookup("EducationBranches", "EducationBranch");

        // ---- SOCIAL SECURITY -------------------------------------------------------------------------
        // The entire chain was absent from this plan, so all six tables were empty and NO employee had any
        // social security deducted. Found by the migrator pre-flight check (cross-reference every
        // IRepository<T> the engines inject against target tables that are empty while legacy has live rows),
        // not by comparing registers -- the five validation employees would have had to carry a deduction for
        // a diff to reveal it.
        //
        // Strict dependency order below; every FK here is NOT NULL except SocialSecurityRules.MaritalStatusId,
        // so a mis-ordered spec drops rows silently rather than nulling a column.
        //   SocialSecurities                  -> Nationalities (above)
        //   SocialSecurityDetails             -> SocialSecurities
        //   SocialSecurityRules               -> SocialSecurities, Qualifications (L0), MaritalStatuses (L0)
        //   SocialSecurityEducationAllowances -> SocialSecurities, Educations, EducationBranches
        //   SocialSecurityMaritalAllowances   -> SocialSecurities, MaritalStatuses, EducationBranches
        Lookup("SocialSecurities", "SocialSecurity");
        Lookup("SocialSecurityDetails", "SocialSecurityDetail");
        Lookup("SocialSecurityRules", "SocialSecurityRule");
        Lookup("SocialSecurityEducationAllowances", "SocialSecurityEducationAllowance");
        Lookup("SocialSecurityMaritalAllowances", "SocialSecurityMaritalAllowance");

        // AbsenceTypes.WageTypeId is an UNCONSTRAINED logical FK -- the single worst instance of the defect
        // TableSpec.LogicalForeignKeys describes. It is the master-table dispatch for the WHOLE leave-pay
        // family (legacy Program.cs line 2674 builds wtLeavePayWageList from AbsenceType.WageType.Oid), so
        // copying the legacy OID through unmapped silently killed every one of them at once: 1003 Annual
        // Paid Leave, 2016 Unapproved Absence Leave, 1047 Business Meeting Leave, 2007 Unpaid Leave,
        // 2020 Annual Leave (No quota). Observed values were legacy OIDs 7/80/82/199/201/217 against HCMv3
        // WageTypes ids 976-1300 -- not one resolved.
        Lookup("AbsenceTypes", "AbsenceType", s => s.LogicalForeignKeys["WageTypeId"] = "WageTypes");

        // The WageType <-> AbsenceType join table, absent from this plan entirely, so the target table held
        // ZERO rows against legacy's 230. It is the sole input to legacy's `includeInPaidLeaveWageTypes`
        // (EmployeeCalculatedPayService.cs lines 2290 and 2328:
        // `WageTypeList.Where(r => r.WageTypeLeave.Any(d => d.AbsenceType.Oid == Program.AnnualLeaveOid))`),
        // which is the wage-type set whose Rate is summed into `totalBasicPay` for BOTH annual-leave
        // provisions. With the table empty the sum was over an empty set, so wage codes 3001 (Annual Leave
        // Provision) and 3004 (Total Annual Leave Provision) computed Amount = 0 and were then dropped by
        // the zero-means-delete -- employee 3400 June 2024 produced neither line against legacy's 17.50 and
        // 116.51. The day-count inputs (4010/4022) were already exact; only this join was missing.
        //
        // MUST follow BOTH WageTypes and AbsenceTypes: WageTypeId and AbsenceTypeId are NOT NULL and both
        // carry real FK constraints, so an unresolved FK DROPS the row silently rather than nulling it.
        Lookup("WageTypeLeaves", "WageTypeLeave");

        // Needed only so EmployeeSubGroups.TicketClassId below has something to resolve against; a clean
        // Code/NameEn/NameAr root lookup with no outgoing FKs of its own.
        Lookup("TicketClasses", "TicketClass");

        Lookup("TicketTypes", "TicketType");
        Lookup("Duties", "Duty");

        // EmployeeSubGroups.TicketClassId: same unconstrained-FK defect. All 7 rows held the legacy OID 3.
        Lookup("EmployeeSubGroups", "EmployeeSubGroup", s => s.LogicalForeignKeys["TicketClassId"] = "TicketClasses");

        Lookup("Holidays", "Holiday");

        // AccountInfos is deliberately NOT migrated. It entered the FK closure only through the nullable
        // PayrollAreas.FIBankId, and its own AccountTypeId is a NOT NULL FK to PayrollAccountTypes -- a table
        // that has no counterpart at all in the legacy schema. Migrating AccountInfo would therefore mean
        // inventing account types. PayrollAreas.FIBankId is left NULL instead (reported per run), which no
        // part of the Time or Payroll engine reads.

        // ---------- LEVEL 2 ------------------------------------------------------------------------
        // AttendanceTypes.WageTypeId: same unconstrained-FK defect, and the attendance-pay dispatch (legacy
        // Program.cs line 2679, wtAttendanceWageList). No damage in the reference database only because
        // legacy's own AttendanceType.WageType happens to be NULL for all three rows -- verified, not
        // assumed. Declared anyway: it is latent, not absent.
        Lookup("AttendanceTypes", "AttendanceType", s => s.LogicalForeignKeys["WageTypeId"] = "WageTypes");
        Lookup("DutyPeriods", "DutyPeriod");
        Lookup("HolidayPeriods", "HolidayPeriod");
        Lookup("PayrollAreas", "PayrollArea");
        Lookup("EmployeeSubGroupAbsences", "EmployeeSubGroupAbsence");
        Lookup("EmployeeSubGroupAbsenceTypes", "EmployeeSubGroupAbsenceType");
        Lookup("EmployeeSubGroupGratuities", "EmployeeSubGroupGratuity");
        // EmployeeSubGroupPayments.WageTypeId: same unconstrained-FK defect. NOT in the originally reported
        // set -- found by sweeping every migrated table for *Id columns with no FK constraint. The target
        // table is empty today, so nothing is corrupt yet; it would have been on the next run.
        Lookup("EmployeeSubGroupPayments", "EmployeeSubGroupPayment", s => s.LogicalForeignKeys["WageTypeId"] = "WageTypes");

        // EmployeeSubGroupTickets.TicketTypeId: same defect. All 5 rows held legacy OIDs 1-5 against HCMv3
        // TicketTypes ids 16-20.
        Lookup("EmployeeSubGroupTickets", "EmployeeSubGroupTicket", s => s.LogicalForeignKeys["TicketTypeId"] = "TicketTypes");
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

        // On-hold flags. Absent from this plan entirely until now: 0 rows migrated against 345 in legacy.
        //
        // This is the most dangerous of the missing-table defects found so far, because it does not remove a
        // LINE from the register -- it removes a GATE. PayrollEngineService injects IRepository<EmployeeOnHold>
        // and uses it for the AllEarnings, AllDeductions, LeavePay and BasicPay holds (legacy Program.cs 2545,
        // 2555, and the INVERTED basic-pay hold at 2538). With the table empty, IsOnHold is false for every
        // employee, every hold silently never fires, and an employee who should have been withheld is paid in
        // full. Nothing is logged, and no register line is missing to notice.
        //
        // It is also invisible to per-employee comparison: none of the five validation employees (2362, 3400,
        // 3804, 4225, 4528) has an on-hold row, so no amount of diffing their registers against legacy would
        // ever have surfaced it. It was found by a migrator pre-flight check that cross-references every
        // IRepository<T> the engines inject against target tables that are empty while legacy has rows.
        //
        // Must follow EmployeeActions (EmployeeActionId is a nullable FK into it -- unresolved would silently
        // NULL, exactly the OvertimeTypes failure mode) and Employees (NOT NULL, unresolved drops the row).
        PerEmployee("EmployeeOnHolds", "EmployeeOnHold");

        // The per-employee end of the social security chain (the five shared tables are at LEVEL 1 above).
        // Absent from this plan until now, so no employee had a social security record and therefore none
        // had the deduction applied.
        PerEmployee("EmployeeSocialSecurities", "EmployeeSocialSecurity");

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

        // Recurring (open-ended, per-month) payments. Absent from this plan entirely until now, so the table
        // was empty and every recurring wage type produced nothing -- e.g. wage code 2028 "Accommodation
        // Rent", which is NOT an accommodation-module calculation at all ("2028" appears nowhere in
        // HCM10.PayrollEngine): it is one EmployeeRecurringPayment row that
        // EmployeePaymentService's PayInterval.Monthly branch (legacy EmployeePaymentService.cs lines
        // 231-233) sums straight into the register.
        //
        // Must follow WageTypes (WageTypeId is NOT NULL here, so an unresolved FK DROPS the row rather than
        // nulling it) and Employees. Both precede it.
        //
        // PayInterval: legacy's column is NULLABLE but its XPO property is a NON-nullable `PayInterval` enum
        // (HCM10.Module/BusinessObjects/PersonnelAdministration/EmployeeRecurringPayment.cs lines 69-75,
        // enum declared in EmployeeBasicPay.cs lines 23-30), so XPO itself materialises NULL as
        // default(PayInterval) == Monthly == 0 -- which is exactly why legacy's own
        // `r.PayInterval == PayInterval.Monthly` predicate matches those rows. ColumnMapper's
        // ISNULL(...,0) coercion therefore REPRODUCES legacy's read semantics; it is not a guess.
        PerEmployee("EmployeeRecurringPayments", "EmployeeRecurringPayment");

        // One-off payments and deductions. Also absent from this plan entirely until now: 0 rows migrated
        // against 119 in legacy for the five validation employees.
        //
        // The clearest casualty is wage code 2005 "Advance Payment Ded". Nothing DISPATCHES on 2005 -- see
        // IPayrollAdvancePaymentService -- because it is not calculated at payroll time at all. An off-cycle
        // advance WRITES an EmployeePayment row carrying wage type 2005, and a later Regular run simply reads
        // it back as an ordinary payment. With the table empty there was nothing to read, so the recovery
        // silently never happened.
        //
        // That is a materially wrong payslip, not a rounding difference: employee 2362 has a 247.384615 row
        // (PaymentDate 2024-06-01, OffCyclePaymentDate 2024-06-27) against a 168.00 basic salary, so legacy's
        // June net pay is essentially zeroed by the recovery while ours would pay the employee in full.
        //
        // Must follow WageTypes and Employees (both NOT NULL FKs here, so an unresolved reference DROPS the
        // row outright rather than nulling a column); CostCenterId and CurrencyId are nullable FKs into
        // CostCenters/Currencies, which also precede this. All four are constrained in the target, so
        // ColumnMapper sees them via sys.foreign_keys and no LogicalForeignKeys entry is needed.
        PerEmployee("EmployeePayments", "EmployeePayment");

        // Wage code 2001 "Loan Recovery Deduction" reads these three, and all three were absent from this
        // plan entirely -- so the tables were empty and the deduction was 0 for every employee.
        // EmployeeLoans depends on Employees (NOT NULL) and LoanTypes (NOT NULL); both precede it.
        PerEmployee("EmployeeLoans", "EmployeeLoan");
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

        // The loan children, scoped through their already-migrated parent loan.
        //
        // EmployeeLoanRecoveries is NOT merely historical: the calculator re-derives BalanceDueAmount from
        // the sum of these rows on every run (legacy EmployeeCalculatedPayService.cs line 1600), so without
        // them every loan looks untouched and the FULL FixedAmount is deducted instead of the remaining
        // balance -- e.g. employee 3400's June-2024 installment is 20.000, not the 25.000 FixedAmount,
        // precisely because 16 earlier recoveries of 25.000 leave only 20.000 of the 420.000 owing.
        plan.Add(new TableSpec
        {
            Target = "EmployeeLoanRecoveries",
            Source = "EmployeeLoanRecovery",
            Scope = ScopeKind.Parent,
            ScopeColumn = "EmployeeLoan",
            ScopeParentEntity = "EmployeeLoans"
        });

        // An exemption suppresses one month's recovery entirely (legacy lines 1615-1621). Omitting it would
        // over-deduct, silently.
        plan.Add(new TableSpec
        {
            Target = "EmployeeLoanExemptions",
            Source = "EmployeeLoanExemption",
            Scope = ScopeKind.Parent,
            ScopeColumn = "EmployeeLoan",
            ScopeParentEntity = "EmployeeLoans"
        });

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
