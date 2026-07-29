using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.ProjectManagement.Services;

/// <summary>
/// Legacy <c>uscProjectCosting.RunProjectCalculation</c> (lines 672-1287) -- the money-bearing half of the
/// Project Cost Engine.
///
/// <para><b>Do not "improve" any arithmetic in this file.</b> Every division, every missing
/// divide-by-zero guard and every <c>+=</c> is legacy's. In particular legacy divides by
/// <c>SalaryRegisterDetail.Number</c> without a zero guard in three places (lines 772, 803, 1124); the
/// outer <c>Amount != 0 || Number != 0</c> gate is the only thing standing between it and an infinity, and
/// that is preserved rather than tightened -- tightening it would change costed amounts.</para>
///
/// <para><b>QUERY-HOISTING (only) deviation:</b> legacy re-queried the AbsenceType / OvertimeType /
/// TicketType wage-type id lists inside the per-project-detail loop (lines 704-706, 727-729, 744). They are
/// loop-invariant master data, so this port loads them once per employee. Behaviour is identical; only the
/// query count changes.</para>
/// </summary>
public partial class ProjectCostEngineService
{
    /// <summary>Legacy line 762 etc. -- Annual Paid Leave.</summary>
    private const string WageCodeAnnualPaidLeave = "1003";

    /// <summary>Legacy line 923 -- the wage type whose Number is "total days in the period".</summary>
    private const string WageCodeTotalDays = "1000";

    /// <summary>Legacy line 831 -- Annual Leave Accrual.</summary>
    private const string WageCodeAnnualLeaveAccrual = "4010";

    /// <summary>Legacy line 831 -- Annual Leave Provision.</summary>
    private const string WageCodeAnnualLeaveProvision = "3001";

    /// <summary>Legacy line 854 -- Indemnity Accrual.</summary>
    private const string WageCodeIndemnityAccrual = "4004";

    /// <summary>Legacy line 854 -- Indemnity Provision.</summary>
    private const string WageCodeIndemnityProvision = "3000";

    /// <summary>Legacy line 915 -- Net Pay.</summary>
    private const string WageCodeNetPay = "9000";

    private async Task RunProjectCalculationAsync(
        SalaryRegister salreg,
        ProjectRegister projectRegister,
        Employee emp,
        DateTime payDate,
        ProjectCostEngineSettings settings)
    {
        // Legacy lines 677-679: the project assignment is looked up AGAIN here, keyed on the register's
        // Period rather than payDate. Preserved (they are the same value on the live path).
        var empProject = _employeeProjectRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeId == emp.Id
                                 && r.StartDate <= projectRegister.Period
                                 && r.EndDate >= projectRegister.Period);

        if (empProject == null)
        {
            // Legacy line 681: the whole method is a no-op when there is no assignment.
            return;
        }

        var wageTypes = _wageTypeRepository.GetAll().ToList().ToDictionary(w => w.Id);

        var salaryRegisterDetails = _salaryRegisterDetailRepository.GetAll()
            .Where(r => r.SalaryRegisterId == salreg.Id)
            .ToList();

        var projectDetails = _employeeProjectDetailRepository.GetAll()
            .Where(r => r.EmployeeProjectId == empProject.Id)
            .ToList();

        if (empProject.CostingMethod == ProjectCostingMethod.ActualDays)
        {
            await RunActualDaysAsync(
                salreg, projectRegister, emp, payDate, settings,
                empProject, projectDetails, salaryRegisterDetails, wageTypes);
        }
        else if (empProject.CostingMethod == ProjectCostingMethod.Percentage)
        {
            await RunPercentageAsync(
                projectRegister, projectDetails, salaryRegisterDetails, wageTypes);
        }

        // Legacy lines 1276-1279: the ActualHours branch is an EMPTY block in legacy. Preserved as a
        // no-op, deliberately NOT a NotImplementedException -- legacy silently produces no cost rows for
        // it and any employee assigned that method must keep behaving the same way.
    }

    #region << ProjectCostingMethod = ActualDays (legacy lines 683-1234) >>

    private async Task RunActualDaysAsync(
        SalaryRegister salreg,
        ProjectRegister projectRegister,
        Employee emp,
        DateTime payDate,
        ProjectCostEngineSettings settings,
        EmployeeProject empProject,
        List<EmployeeProjectDetail> projectDetails,
        List<SalaryRegisterDetail> salaryRegisterDetails,
        Dictionary<int, WageType> wageTypes)
    {
        // Legacy line 686: carries the LAST project detail seen, so the leave block below can fall back to
        // it when the employee has no project assignment in a later month.
        var lastProjectDetail = new List<EmployeeProjectDetail>();

        var wageTypeIdsInRegister = salaryRegisterDetails.Select(r => r.WageTypeId).ToList();

        // Hoisted master data -- see the class remarks.
        var absenceTypes = _absenceTypeRepository.GetAll()
            .Where(r => r.WageTypeId != null)
            .Select(r => new AbsenceTypeWageRef { AbsenceTypeId = r.Id, WageTypeId = r.WageTypeId.Value })
            .ToList();
        var absenceWageTypeIds = absenceTypes.Select(r => r.WageTypeId).ToList();

        var overtimeWageTypeIds = await _overtimeWageTypeResolver.GetOvertimeWageTypeIdsAsync();

        var airTicketWageTypeIds = _ticketTypeRepository.GetAll()
            .Where(r => r.WageTypeId != null)
            .Select(r => r.WageTypeId.Value)
            .ToList();

        #region << Calculate all project register lines for all wage types (legacy 688-993) >>

        foreach (var projectDetail in projectDetails)
        {
            lastProjectDetail.Clear();
            lastProjectDetail.Add(projectDetail);

            var projWageTypes = GetProjectWageTypes(projectDetail.ProjectId, wageTypeIdsInRegister, wageTypes);

            var empTimeRegister = LoadTimeRegisterRows(
                emp.Id, projectDetail.StartDate, projectDetail.EndDate, settings);

            // Legacy lines 732-741.
            var empOverTime = _employeeOvertimeRepository.GetAll()
                .Where(r => r.EmployeeId == emp.Id
                            && r.EffectiveDate >= projectDetail.StartDate
                            && r.EffectiveDate <= projectDetail.EndDate)
                .Select(r => new { r.OvertimeTypeId, r.ApprovedOvertimeHours })
                .ToList();

            foreach (var projWageType in projWageTypes)
            {
                var wageType = wageTypes[projWageType.WageTypeId];

                var salaryRegisterDetail = salaryRegisterDetails
                    .FirstOrDefault(r => r.WageTypeId == projWageType.WageTypeId);

                if (salaryRegisterDetail == null)
                {
                    continue;
                }

                if (salaryRegisterDetail.Amount == 0 && salaryRegisterDetail.Number == 0)
                {
                    continue;
                }

                double number = 0;
                double amount = 0;

                if (absenceWageTypeIds.Contains(projWageType.WageTypeId))
                {
                    #region << WageType = AbsenceType (legacy 758-788) >>

                    var leaveTypeId = absenceTypes
                        .Where(r => r.WageTypeId == projWageType.WageTypeId)
                        .Select(r => r.AbsenceTypeId)
                        .FirstOrDefault();

                    if (wageType.Code == WageCodeAnnualPaidLeave)
                    {
                        var leavedays = empTimeRegister.Where(r => r.LeaveTypeId == leaveTypeId).Sum(r => r.LeaveDay);
                        var leaveAdjdays = empTimeRegister.Where(r => r.LeaveTypeId == leaveTypeId).Sum(r => r.LeaveAdjDay);
                        leavedays = leavedays + leaveAdjdays;

                        if (leavedays != 0)
                        {
                            number = leavedays;
                            amount = (salaryRegisterDetail.Amount / salaryRegisterDetail.Number) * leavedays;
                        }
                    }
                    else
                    {
                        // Legacy 775-786: every other leave is split EQUALLY across the projects.
                        double leavedays = salaryRegisterDetail.Number;
                        double leaveAmt = salaryRegisterDetail.Amount;
                        double noOfProject = projectDetails.Count;

                        if (noOfProject != 0)
                        {
                            number = leavedays / noOfProject;
                            amount = leaveAmt / noOfProject;
                        }
                    }

                    #endregion
                }
                else if (wageType.IncludeInBasicPay)
                {
                    #region << WageType = IncludeInBasicPay (legacy 790-806) >>

                    var actualWorkingDays = SumActualWorkingDays(empTimeRegister);
                    if (actualWorkingDays != 0)
                    {
                        number = actualWorkingDays;
                        amount = (salaryRegisterDetail.Amount / salaryRegisterDetail.Number) * actualWorkingDays;
                    }

                    #endregion
                }
                else if (overtimeWageTypeIds.Contains(projWageType.WageTypeId))
                {
                    #region << WageType = Overtime (legacy 808-819) >>

                    // Unreachable while IProjectCostOvertimeWageTypeResolver is the null implementation --
                    // see that file (REPORTED GAP).
                    var overTimeType = await _overtimeWageTypeResolver
                        .GetOvertimeTypeForWageTypeAsync(projWageType.WageTypeId);

                    if (overTimeType != null)
                    {
                        var approvedOvertimeHours = empOverTime
                            .Where(r => r.OvertimeTypeId == overTimeType.OvertimeTypeId)
                            .Sum(r => r.ApprovedOvertimeHours);

                        var otHours = approvedOvertimeHours * overTimeType.OvertimeRate;
                        number = otHours;
                        amount = otHours * salaryRegisterDetail.Rate;
                    }

                    #endregion
                }
                else if (airTicketWageTypeIds.Contains(projWageType.WageTypeId))
                {
                    #region << WageType = Air Ticket (legacy 821-828) >>

                    // Taken whole, NOT pro-rated -- legacy assigns the full register figures.
                    number = salaryRegisterDetail.Number;
                    amount = salaryRegisterDetail.Amount;

                    #endregion
                }
                else if (wageType.Code == WageCodeAnnualLeaveAccrual || wageType.Code == WageCodeAnnualLeaveProvision)
                {
                    #region << WageType = Annual Leave Accrual / Provision (legacy 830-851) >>

                    if (wageType.Code == WageCodeAnnualLeaveAccrual)
                    {
                        number = empTimeRegister.Sum(r => r.AnnualLeaveAccrualPerDay);
                        amount = 0;
                    }
                    else
                    {
                        var totalAccrual = empTimeRegister.Sum(r => r.AnnualLeaveAccrualPerDay);
                        double totaldays = salaryRegisterDetail.Number;
                        double totalamount = salaryRegisterDetail.Amount;

                        if (totaldays != 0)
                        {
                            number = totalAccrual;
                            amount = (totalAccrual * totalamount) / totaldays;
                        }
                    }

                    #endregion
                }
                else if (wageType.Code == WageCodeIndemnityAccrual || wageType.Code == WageCodeIndemnityProvision)
                {
                    #region << WageType = Indemnity Accrual / Provision (legacy 853-909) >>

                    // NOTE, legacy asymmetry preserved: TotalMonthDays is counted against the RAW
                    // TimeRegister.AbsentDay column (legacy queries the entity directly, lines 860-866 /
                    // 883-889) while TotalProjectdays is counted against the APPROVAL-SWITCHED projection.
                    var monthStartDate = new DateTime(payDate.Year, payDate.Month, 1);
                    var monthEndDate = new DateTime(
                        payDate.Year, payDate.Month, DateTime.DaysInMonth(payDate.Year, payDate.Month));

                    var totalMonthDays = _timeRegisterRepository.GetAll()
                        .Count(r => r.EmployeeId == emp.Id
                                    && r.DayDate >= monthStartDate
                                    && r.DayDate <= monthEndDate
                                    && r.AbsentDay == 0
                                    && r.UnpaidLeaveDay == 0);

                    var totalIndemnityDays = salaryRegisterDetails
                        .Where(r => wageTypes[r.WageTypeId].Code == WageCodeIndemnityAccrual)
                        .Select(r => r.Number)
                        .FirstOrDefault();

                    var totalProjectdays = empTimeRegister.Count(r => r.AbsentDay == 0 && r.UnpaidLeaveDay == 0);

                    if (totalMonthDays != 0)
                    {
                        var indemnityDays = totalProjectdays * totalIndemnityDays / totalMonthDays;

                        if (wageType.Code == WageCodeIndemnityAccrual)
                        {
                            number = indemnityDays;
                            amount = 0;
                        }
                        else
                        {
                            var provisionLine = salaryRegisterDetails
                                .FirstOrDefault(r => wageTypes[r.WageTypeId].Code == WageCodeIndemnityProvision);

                            // Legacy line 899 dereferences this without a null check; the branch is only
                            // reached because the 3000 line IS the current register line, so it exists.
                            var totalamount = provisionLine != null ? provisionLine.Amount : 0;

                            if (totalIndemnityDays != 0)
                            {
                                number = indemnityDays;
                                amount = (indemnityDays * totalamount) / totalIndemnityDays;
                            }
                        }
                    }

                    #endregion
                }
                else if (wageType.BillingType == BillingType.Earning && salaryRegisterDetail.Number == 0)
                {
                    #region << WageType = Earning (legacy 911-940) >>

                    // Legacy 914-918: when this is the 9000 Net Pay line AND net pay is calculated
                    // project-wise, NOTHING is done here -- the 9000 line is written by the dedicated block
                    // at the end instead.
                    if (!(wageType.Code == WageCodeNetPay && settings.CalculateNetPayProjectWise))
                    {
                        var totalDays = salaryRegisterDetails
                            .Where(r => wageTypes[r.WageTypeId].Code == WageCodeTotalDays)
                            .Select(r => r.Number)
                            .FirstOrDefault();

                        var actualWorkingDays = SumActualWorkingDays(empTimeRegister);
                        if (actualWorkingDays != 0)
                        {
                            number = actualWorkingDays;
                            if (totalDays != 0)
                            {
                                amount = (salaryRegisterDetail.Amount / totalDays) * actualWorkingDays;
                            }
                        }
                    }

                    #endregion
                }
                else if (wageType.BillingType == BillingType.Deduction && salaryRegisterDetail.Number == 0)
                {
                    #region << WageType = Deduction (legacy 942-963) >>

                    var totalDays = salaryRegisterDetails
                        .Where(r => wageTypes[r.WageTypeId].Code == WageCodeTotalDays)
                        .Select(r => r.Number)
                        .FirstOrDefault();

                    var actualWorkingDays = SumActualWorkingDays(empTimeRegister);
                    if (actualWorkingDays != 0)
                    {
                        number = actualWorkingDays;
                        if (totalDays != 0)
                        {
                            amount = (salaryRegisterDetail.Amount / totalDays) * actualWorkingDays;
                        }
                    }

                    #endregion
                }

                // Legacy 966-988.
                if (amount != 0 || number != 0)
                {
                    await UpsertProjectRegisterDetailAsync(
                        projectRegister, projWageType.ProjectId, projWageType.WageTypeId, number, amount);
                }
            }
        }

        #endregion

        await RunLeavePaidFullWithPayrollAsync(
            salreg, projectRegister, emp, settings,
            lastProjectDetail, salaryRegisterDetails, wageTypes, absenceWageTypeIds, absenceTypes);

        await RunNetPayProjectWiseAsync(
            projectRegister, emp, settings, projectDetails, wageTypes);
    }

    #endregion

    #region << LeavePaymentType == PayFullWithPayroll (legacy 995-1168) >>

    /// <summary>
    /// Legacy's "carry a fully-paid leave that spills into later months back onto this period's project
    /// register" block. Only the 1003 Annual Paid Leave wage type produces a figure inside it (legacy
    /// 1082-1128); every other wage type falls through with number=amount=0 and writes nothing.
    /// </summary>
    private async Task RunLeavePaidFullWithPayrollAsync(
        SalaryRegister salreg,
        ProjectRegister projectRegister,
        Employee emp,
        ProjectCostEngineSettings settings,
        List<EmployeeProjectDetail> lastProjectDetail,
        List<SalaryRegisterDetail> salaryRegisterDetails,
        Dictionary<int, WageType> wageTypes,
        List<int> absenceWageTypeIds,
        List<AbsenceTypeWageRef> absenceTypes)
    {
        // Legacy 998-999.
        var startdate = new DateTime(projectRegister.Period.Year, projectRegister.Period.Month, 1, 0, 0, 0);
        var enddate = new DateTime(
            projectRegister.Period.Year, projectRegister.Period.Month, projectRegister.Period.Day, 0, 0, 0);

        // Legacy 1001-1010.
        var empAbsFullPay = _employeeAbsenceRepository.GetAll()
            .Where(r => r.EmployeeId == emp.Id
                        && r.AbsenceType != null
                        && r.AbsenceHours == 0
                        && r.AbsenceType.IsPaid
                        && r.PaymentDate >= startdate
                        && r.PaymentDate <= enddate
                        && r.LeavePaymentType == LeavePaymentType.PayFullWithPayroll)
            .Select(r => new { r.StartDate, r.EndDate, WageTypeId = r.AbsenceType.WageTypeId })
            .FirstOrDefault();

        if (empAbsFullPay == null || empAbsFullPay.WageTypeId == null)
        {
            return;
        }

        var absenceWageTypeId = empAbsFullPay.WageTypeId.Value;

        var wageTypeIdsInRegister = salaryRegisterDetails.Select(r => r.WageTypeId).ToList();

        // Legacy 1017-1020: the absence's own wage type plus anything that arrears off it.
        var wageTypesList = _wageTypeRepository.GetAll()
            .Where(r => r.Id == absenceWageTypeId
                        || (r.ArrearSourceWageTypeId != null && r.ArrearSourceWageTypeId == absenceWageTypeId))
            .Select(r => r.Id)
            .ToList();

        // Legacy 1025-1026: start from the FIRST DAY OF THE MONTH AFTER the absence starts.
        var loopStartDate = new DateTime(empAbsFullPay.StartDate.Year, empAbsFullPay.StartDate.Month, 1).AddMonths(1);
        var loopEndDate = MonthEnd(loopStartDate);

        while (loopStartDate <= empAbsFullPay.EndDate)
        {
            // Legacy 1037-1045.
            var empProjectNext = _employeeProjectRepository.GetAll()
                .FirstOrDefault(r => r.EmployeeId == emp.Id
                                     && r.StartDate <= loopEndDate
                                     && r.EndDate >= loopEndDate);

            if (empProjectNext != null)
            {
                lastProjectDetail.Clear();
                lastProjectDetail.AddRange(_employeeProjectDetailRepository.GetAll()
                    .Where(r => r.EmployeeProjectId == empProjectNext.Id)
                    .ToList());
            }

            var actProjectDetail = lastProjectDetail.ToList();

            foreach (var projectDetail in actProjectDetail)
            {
                lastProjectDetail.Clear();
                lastProjectDetail.Add(projectDetail);

                var projWageTypes = GetProjectWageTypes(
                    projectDetail.ProjectId,
                    wageTypeIdsInRegister.Where(id => wageTypesList.Contains(id)).ToList(),
                    wageTypes);

                foreach (var projWageType in projWageTypes)
                {
                    var wageType = wageTypes[projWageType.WageTypeId];

                    var salaryRegisterDetail = salaryRegisterDetails
                        .FirstOrDefault(r => r.WageTypeId == projWageType.WageTypeId);

                    if (salaryRegisterDetail == null)
                    {
                        continue;
                    }

                    if (salaryRegisterDetail.Amount == 0 && salaryRegisterDetail.Number == 0)
                    {
                        continue;
                    }

                    double number = 0;
                    double amount = 0;

                    if (absenceWageTypeIds.Contains(projWageType.WageTypeId)
                        && wageType.Code == WageCodeAnnualPaidLeave)
                    {
                        var leaveTypeId = absenceTypes
                            .Where(r => r.WageTypeId == projWageType.WageTypeId)
                            .Select(r => r.AbsenceTypeId)
                            .FirstOrDefault();

                        // Legacy 1090-1102: when the employee HAS a project next month, use that project
                        // detail's own range; otherwise use the loop month, capped at the absence end.
                        var getPeriodStart = empProjectNext != null ? projectDetail.StartDate : loopStartDate;
                        var getPeriodEnd = empProjectNext != null ? projectDetail.EndDate : loopEndDate;
                        getPeriodEnd = getPeriodEnd > empAbsFullPay.EndDate ? empAbsFullPay.EndDate : getPeriodEnd;

                        var rows = LoadTimeRegisterRows(emp.Id, getPeriodStart, getPeriodEnd, settings);

                        var leavedays = rows.Where(r => r.LeaveTypeId == leaveTypeId).Sum(r => r.LeaveDay);
                        var leaveAdjdays = rows.Where(r => r.LeaveTypeId == leaveTypeId).Sum(r => r.LeaveAdjDay);
                        leavedays = leavedays + leaveAdjdays;

                        if (leavedays != 0)
                        {
                            number = leavedays;
                            amount = (salaryRegisterDetail.Amount / salaryRegisterDetail.Number) * leavedays;
                        }
                    }

                    if (amount != 0 || number != 0)
                    {
                        await UpsertProjectRegisterDetailAsync(
                            projectRegister, projWageType.ProjectId, projWageType.WageTypeId, number, amount);
                    }
                }
            }

            // Legacy 1160-1161.
            loopStartDate = loopStartDate.AddMonths(1);
            loopEndDate = MonthEnd(loopStartDate);
        }
    }

    #endregion

    #region << CalculateNetPayProjectWise = True (legacy 1170-1231) >>

    private async Task RunNetPayProjectWiseAsync(
        ProjectRegister projectRegister,
        Employee emp,
        ProjectCostEngineSettings settings,
        List<EmployeeProjectDetail> projectDetails,
        Dictionary<int, WageType> wageTypes)
    {
        if (!settings.CalculateNetPayProjectWise)
        {
            return;
        }

        var netPayWageType = _wageTypeRepository.GetAll().FirstOrDefault(r => r.Code == WageCodeNetPay);
        if (netPayWageType == null)
        {
            // Legacy dereferences this at line 1218 without a null check; if the tenant has no 9000 wage
            // type there is nothing to write.
            return;
        }

        var projectIds = projectDetails.Select(r => r.ProjectId).Distinct().ToList();

        foreach (var projectId in projectIds)
        {
            double number = 0;

            // Legacy 1179-1199: the working-day count is accumulated ACROSS every detail row for the
            // project (note `number = number + ...`, not a reset per detail).
            foreach (var projectDetail in projectDetails.Where(r => r.ProjectId == projectId))
            {
                var rows = LoadTimeRegisterRows(emp.Id, projectDetail.StartDate, projectDetail.EndDate, settings);
                number = number + SumActualWorkingDays(rows);
            }

            // Legacy 1201-1205: net pay = sum of this project's earning lines that impact net pay.
            var details = _projectRegisterDetailRepository.GetAll()
                .Where(r => r.ProjectRegisterId == projectRegister.Id && r.ProjectId == projectId)
                .ToList();

            var amount = details
                .Where(r => wageTypes[r.WageTypeId].Code != WageCodeNetPay
                            && wageTypes[r.WageTypeId].BillingType == BillingType.Earning
                            && !wageTypes[r.WageTypeId].DontImpactNetPay)
                .Sum(r => r.Amount);

            if (amount != 0 || number != 0)
            {
                await UpsertProjectRegisterDetailAsync(
                    projectRegister, projectId, netPayWageType.Id, number, amount);
            }
        }
    }

    #endregion

    #region << ProjectCostingMethod = Percentage (legacy 1237-1272) >>

    /// <summary>
    /// Legacy's percentage split. NOTE two behaviours preserved deliberately:
    /// (1) it ALWAYS inserts a new detail row -- no dedupe upsert, unlike the ActualDays path, so an
    /// employee assigned twice to the same project gets two rows;
    /// (2) it assigns (<c>=</c>) rather than accumulates (<c>+=</c>).
    /// </summary>
    private async Task RunPercentageAsync(
        ProjectRegister projectRegister,
        List<EmployeeProjectDetail> projectDetails,
        List<SalaryRegisterDetail> salaryRegisterDetails,
        Dictionary<int, WageType> wageTypes)
    {
        var wageTypeIdsInRegister = salaryRegisterDetails.Select(r => r.WageTypeId).ToList();

        foreach (var projectDetail in projectDetails)
        {
            var projWageTypes = GetProjectWageTypes(projectDetail.ProjectId, wageTypeIdsInRegister, wageTypes);

            foreach (var projWageType in projWageTypes)
            {
                var salaryRegisterDetail = salaryRegisterDetails
                    .FirstOrDefault(r => r.WageTypeId == projWageType.WageTypeId);

                if (salaryRegisterDetail == null)
                {
                    continue;
                }

                await _projectRegisterDetailRepository.InsertAsync(new ProjectRegisterDetail
                {
                    TenantId = projectRegister.TenantId,
                    ProjectRegisterId = projectRegister.Id,
                    ProjectId = projWageType.ProjectId,
                    WageTypeId = salaryRegisterDetail.WageTypeId,
                    Number = (salaryRegisterDetail.Number * projectDetail.Percentage) / 100,
                    Amount = (salaryRegisterDetail.Amount * projectDetail.Percentage) / 100
                });
            }
        }

        await CurrentUnitOfWork.SaveChangesAsync();
    }

    #endregion

    #region << Helpers >>

    /// <summary>AbsenceType id + its WageType id -- legacy's two separate AbsenceType queries
    /// (lines 704-706 and 761) collapsed into one loop-invariant list.</summary>
    private sealed class AbsenceTypeWageRef
    {
        public int AbsenceTypeId { get; set; }

        public int WageTypeId { get; set; }
    }

    /// <summary>Legacy's repeated ProjectWageType query, ordered by <c>WageType.PayrollOrder</c>.</summary>
    private List<ProjectWageType> GetProjectWageTypes(
        int projectId,
        List<int> wageTypeIdsInRegister,
        Dictionary<int, WageType> wageTypes)
    {
        return _projectWageTypeRepository.GetAll()
            .Where(r => r.ProjectId == projectId && wageTypeIdsInRegister.Contains(r.WageTypeId))
            .ToList()
            .Where(r => wageTypes.ContainsKey(r.WageTypeId))
            .OrderBy(r => wageTypes[r.WageTypeId].PayrollOrder)
            .ToList();
    }

    /// <summary>
    /// Legacy's TimeRegister projection (lines 709-724), including the
    /// <see cref="ProjectCostEngineSettings.EnableTimeRegisterApproval"/> switch on
    /// <c>ActualWorkingDay</c>/<c>AbsentDay</c> and the <c>LeaveType == null -&gt; 0</c> mapping.
    /// </summary>
    private List<ProjectCostTimeRegisterRow> LoadTimeRegisterRows(
        int employeeId,
        DateTime fromDate,
        DateTime toDate,
        ProjectCostEngineSettings settings)
    {
        return _timeRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.DayDate >= fromDate && r.DayDate <= toDate)
            .Select(r => new ProjectCostTimeRegisterRow
            {
                ActualWorkingDay = !settings.EnableTimeRegisterApproval ? r.ActualWorkingDay : r.ApprovedActualWorkingDay,
                PublicHoliday = r.PublicHoliday,
                RestDay = r.RestDay,
                AbsentDay = !settings.EnableTimeRegisterApproval ? r.AbsentDay : r.ApprovedAbsentDay,
                UnpaidLeaveDay = r.UnpaidLeaveDay,
                LeaveTypeId = r.LeaveTypeId != null ? r.LeaveTypeId.Value : 0,
                LeaveDay = r.LeaveDay,
                LeaveAdjDay = r.LeaveAdjDay,
                AnnualLeaveAccrualPerDay = r.AnnualLeaveAccrualPerDay
            })
            .ToList();
    }

    /// <summary>
    /// Legacy's repeated "actual working days" expression (lines 794-799, 925-930, 950-955, 1193-1198):
    /// rows that have ANY of working day / public holiday / rest day, summing all three together.
    /// </summary>
    private static double SumActualWorkingDays(List<ProjectCostTimeRegisterRow> rows)
    {
        return rows
            .Where(r => r.ActualWorkingDay != 0 || r.PublicHoliday == 1 || r.RestDay == 1)
            .Sum(r => r.ActualWorkingDay + r.PublicHoliday + r.RestDay);
    }

    /// <summary>
    /// Legacy's "Create ProjectRegisterDetail Entry" region (lines 966-988 / 1130-1152 / 1207-1228):
    /// find-or-create keyed on (Project, WageType) and ACCUMULATE with <c>+=</c>, committing immediately
    /// so the next iteration sees the row.
    /// </summary>
    private async Task UpsertProjectRegisterDetailAsync(
        ProjectRegister projectRegister,
        int projectId,
        int wageTypeId,
        double number,
        double amount)
    {
        var detail = _projectRegisterDetailRepository.GetAll()
            .FirstOrDefault(r => r.ProjectRegisterId == projectRegister.Id
                                 && r.ProjectId == projectId
                                 && r.WageTypeId == wageTypeId);

        if (detail == null)
        {
            detail = new ProjectRegisterDetail
            {
                TenantId = projectRegister.TenantId,
                ProjectRegisterId = projectRegister.Id,
                ProjectId = projectId,
                WageTypeId = wageTypeId,
                Number = number,
                Amount = amount
            };

            await _projectRegisterDetailRepository.InsertAsync(detail);
        }
        else
        {
            detail.Number += number;
            detail.Amount += amount;
            await _projectRegisterDetailRepository.UpdateAsync(detail);
        }

        // Legacy line 984: CommitChanges + ProjectRegisterDetail.Reload() -- the accumulation depends on
        // the row being visible to the very next lookup.
        await CurrentUnitOfWork.SaveChangesAsync();
    }

    #endregion
}
