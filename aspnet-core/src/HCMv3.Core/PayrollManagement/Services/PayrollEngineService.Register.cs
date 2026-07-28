using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// <see cref="SalaryRegister"/> header creation/refresh half of <see cref="PayrollEngineService"/> --
/// the port of legacy <c>HCM10.PayrollEngine/Program.cs</c> lines 943-1181 (Regular) and 1183-1384
/// (OffCycle).
/// </summary>
public partial class PayrollEngineService
{
    #region << Regular (legacy lines 943-1181) >>

    private async Task ProcessRegularEmployeeAsync(
        PayrollEngineRunInput input,
        Employee employee,
        DateTime payInDate,
        PayrollEngineSettings settings,
        PayrollOnHoldFlags onHold,
        EmployeeOnHold onHoldRecord,
        bool isNotReturnFromLeave,
        EmployeeAbsence notReturnAbsence,
        EmployeePayrollStatus payrollStatus,
        PayrollArea payrollArea,
        List<EmployeeOrgAssignment> orgAssignments,
        PayrollEmployeeRunResult employeeResult)
    {
        #region << LockSalaryRegisterOnPayrollExit (legacy lines 946-958) >>

        if (settings.LockSalaryRegisterOnPayrollExit)
        {
            var isLocked = _salaryRegisterRepository.GetAll().Any(r => r.EmployeeId == employee.Id
                                                                       && r.ForPeriod == payInDate
                                                                       && r.InPeriod == payInDate
                                                                       && r.PayrollRunType == PayrollRunType.Regular
                                                                       && r.Locked);
            if (isLocked)
            {
                throw new UserFriendlyException(string.Format(
                    "Regular salary register locked. ForPeriod : {0}", payInDate.ToString("dd/MM/yyyy")));
            }
        }

        #endregion

        // Legacy lines 960-965.
        if (onHoldRecord != null && onHold.All)
        {
            throw new UserFriendlyException(string.Format(
                "Salary On Hold for Employee : {0}, From : {1} To : {2}",
                employee.Code,
                onHoldRecord.StartDate.ToString("dd/MM/yyyy"),
                onHoldRecord.EndDate.ToString("dd/MM/yyyy")));
        }

        var periods = CollectPayrollPeriods(
            employee, payrollStatus, payInDate, settings,
            includeMasterDataChangeSeed: true,
            throwWhenBeforeEarliestRetroAccountingDate: true);

        foreach (var payForDate in periods)
        {
            // Legacy lines 1045-1050.
            if (payrollStatus.DoNoAccountAfter < payForDate && payrollStatus.DoNoAccountAfter != new DateTime(1, 1, 1))
            {
                continue;
            }

            var existing = _salaryRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == employee.Id
                            && r.ForPeriod == payForDate
                            && r.InPeriod == payInDate
                            && r.PayrollRunType == PayrollRunType.Regular)
                .ToList();

            if (existing.Count > 1)
            {
                // Legacy line 1062 (its own format string is malformed -- the placeholder is missing; the
                // date is appended here instead).
                throw new UserFriendlyException(string.Format(
                    "Duplicate Salary Register found for {0}", payForDate.ToShortDateString()));
            }

            SalaryRegister salaryRegister = null;
            var isNewRegister = false;

            if (existing.Count == 1)
            {
                // Legacy lines 1063-1082: reuse, force RunType/ResultType, and delete every detail +
                // day-detail row before recalculating.
                salaryRegister = existing[0];
                salaryRegister.PayrollRunType = PayrollRunType.Regular;
                salaryRegister.PayrollResultType = PayrollResultType.Current;
                await _salaryRegisterRepository.UpdateAsync(salaryRegister);

                await _registerStore.ClearLinesAsync(BuildRegisterRef(salaryRegister, input.RegisterTarget));
            }
            else if (!onHold.All)
            {
                // Legacy lines 1086-1099.
                salaryRegister = new SalaryRegister
                {
                    EmployeeId = employee.Id,
                    ForPeriod = payForDate,
                    InPeriod = payInDate,
                    PayrollRunType = PayrollRunType.Regular,
                    PayrollResultType = PayrollResultType.Current
                };

                // InsertAndGetIdAsync, never InsertAsync: the id is immediately used as the FK of every
                // register line written below, and ABP only assigns it on flush.
                salaryRegister.Id = await _salaryRegisterRepository.InsertAndGetIdAsync(salaryRegister);
                isNewRegister = true;
            }

            if (!onHold.All)
            {
                // NOT PORTED (dead code): legacy computes a `cutoffDate` local at lines 1103-1116 and never
                // reads it again on this path.

                #region << Not-return-from-leave abort (legacy lines 1118-1132) >>

                if (isNotReturnFromLeave && !settings.GenerateRegisterOnNotReturnFromLeave)
                {
                    if (isNewRegister && salaryRegister != null)
                    {
                        await _salaryRegisterRepository.DeleteAsync(salaryRegister);
                    }

                    throw new UserFriendlyException(string.Format(
                        "ReturnDate not found for LeaveType : '{0}', StartDate : '{1}', EndDate : '{2}' For Employee : {3}",
                        notReturnAbsence?.AbsenceType?.NameEn,
                        notReturnAbsence?.StartDate.ToString("dd/MM/yyyy"),
                        notReturnAbsence?.EndDate.ToString("dd/MM/yyyy"),
                        employee.Code));
                }

                #endregion

                // Legacy line 1057: "Resetting PayDate to Retro Pay Date" -- payDate is the RETRO month end,
                // NOT payInDate.
                await RunPayrollCalculationAsync(
                    input, employee, payForDate, payInDate, settings, onHold,
                    isNotReturnFromLeave, payrollArea, orgAssignments,
                    BuildRegisterRef(salaryRegister, input.RegisterTarget));

                employeeResult.SalaryRegisterIds.Add(salaryRegister.Id);
            }

            await ApplyPayrollResultTypeAgeingAsync(employee.Id, payForDate, payInDate);
        }
    }

    #endregion

    #region << OffCycle (legacy lines 1183-1384) >>

    private async Task ProcessOffCycleEmployeeAsync(
        PayrollEngineRunInput input,
        Employee employee,
        DateTime payInDate,
        PayrollEngineSettings settings,
        PayrollOnHoldFlags onHold,
        bool isNotReturnFromLeave,
        EmployeeAbsence notReturnAbsence,
        EmployeePayrollStatus payrollStatus,
        PayrollArea payrollArea,
        List<EmployeeOrgAssignment> orgAssignments,
        PayrollEmployeeRunResult employeeResult)
    {
        #region << LockSalaryRegisterOnPayrollExit (legacy lines 1186-1198) >>

        if (settings.LockSalaryRegisterOnPayrollExit)
        {
            var isLocked = _salaryRegisterRepository.GetAll().Any(r => r.EmployeeId == employee.Id
                                                                       && r.ForPeriod == payInDate
                                                                       && r.InPeriod == payInDate
                                                                       && r.PayrollRunType == PayrollRunType.OffCycle
                                                                       && r.Locked);
            if (isLocked)
            {
                throw new UserFriendlyException(string.Format(
                    "OffCycle salary register locked. ForPeriod : {0}", payInDate.ToString("dd/MM/yyyy")));
            }
        }

        #endregion

        // NOT PORTED: Program.DeleteAdvancePaymentDed (legacy line 1201). Its predicate keys off
        // EmployeePayment.OffCyclePaymentDate, which this port's EmployeePayment does not carry -- see the
        // class remarks on PayrollEngineService for why this is skipped rather than approximated.

        var periods = CollectPayrollPeriods(
            employee, payrollStatus, payInDate, settings,
            // Legacy includes the EarliestMasterDataChange seed on the END-OF-SERVICE arm only (line 1216).
            includeMasterDataChangeSeed: input.OffCycleReason == PayrollOffCycleReasons.EndOfService,
            throwWhenBeforeEarliestRetroAccountingDate: false);

        var reasonTypeId = _payrollReasonTypeRepository.GetAll()
            .Where(r => r.NameEn == input.OffCycleReason || r.NameAr == input.OffCycleReason)
            .Select(r => (int?)r.Id)
            .FirstOrDefault();

        foreach (var payForDate in periods)
        {
            // Legacy lines 1296-1303.
            if (payrollStatus.DoNoAccountAfter < payForDate && payrollStatus.DoNoAccountAfter != new DateTime(1, 1, 1))
            {
                continue;
            }

            var existing = _salaryRegisterRepository.GetAll()
                .Where(r => r.EmployeeId == employee.Id && r.ForPeriod == payForDate && r.InPeriod == payInDate)
                .ToList();

            // Legacy lines 1307-1308: an off-cycle run cannot follow a regular one in the same period.
            if (existing.Any(r => r.PayrollRunType == PayrollRunType.Regular))
            {
                throw new UserFriendlyException("Cannot run OffCycle Payroll for the month,as Regular Payroll already ran.");
            }

            SalaryRegister salaryRegister;
            var isNewRegister = false;

            if (existing.Count == 1)
            {
                // Legacy lines 1310-1328.
                salaryRegister = existing[0];
                if (reasonTypeId != null)
                {
                    salaryRegister.PayrollReasonTypeId = reasonTypeId.Value;
                }

                await _salaryRegisterRepository.UpdateAsync(salaryRegister);
                await _registerStore.ClearLinesAsync(BuildRegisterRef(salaryRegister, input.RegisterTarget));
            }
            else
            {
                // Legacy lines 1330-1346. NOTE the off-cycle arm creates the register regardless of
                // onHold_All -- unlike the Regular arm (line 1086). That asymmetry is legacy's.
                salaryRegister = new SalaryRegister
                {
                    EmployeeId = employee.Id,
                    ForPeriod = payForDate,
                    InPeriod = payInDate,
                    PayrollRunType = PayrollRunType.OffCycle,
                    PayrollResultType = PayrollResultType.Current,
                    OffCyclePaymentDate = input.OffCyclePaymentDate
                };

                if (reasonTypeId != null)
                {
                    salaryRegister.PayrollReasonTypeId = reasonTypeId.Value;
                }

                salaryRegister.Id = await _salaryRegisterRepository.InsertAndGetIdAsync(salaryRegister);
                isNewRegister = true;
            }

            #region << Not-return-from-leave abort (legacy lines 1348-1362) >>

            // NOTE: the off-cycle arm ignores GenerateRegisterOnNotReturnFromLeave entirely and instead
            // exempts only end-of-service runs. Legacy's, preserved.
            if (isNotReturnFromLeave && input.OffCycleReason != PayrollOffCycleReasons.EndOfService)
            {
                if (isNewRegister)
                {
                    await _salaryRegisterRepository.DeleteAsync(salaryRegister);
                }

                throw new UserFriendlyException(string.Format(
                    "ReturnDate not found for LeaveType : '{0}', StartDate : '{1}', EndDate : '{2}' For Employee : {3}",
                    notReturnAbsence?.AbsenceType?.NameEn,
                    notReturnAbsence?.StartDate.ToString("dd/MM/yyyy"),
                    notReturnAbsence?.EndDate.ToString("dd/MM/yyyy"),
                    employee.Code));
            }

            #endregion

            // Legacy line 1365: the off-cycle arm passes ALL SEVEN on-hold flags as false.
            await RunPayrollCalculationAsync(
                input, employee, payForDate, payInDate, settings, new PayrollOnHoldFlags(),
                isNotReturnFromLeave, payrollArea, orgAssignments,
                BuildRegisterRef(salaryRegister, input.RegisterTarget));

            employeeResult.SalaryRegisterIds.Add(salaryRegister.Id);
        }
    }

    #endregion

    #region << Shared helpers >>

    /// <summary>
    /// Legacy's "Change PayrollResultType" region (lines 1149-1165): the employee's registers for the same
    /// <c>ForPeriod</c> but an EARLIER <c>InPeriod</c> are demoted -- the most recent to
    /// <see cref="PayrollResultType.Last"/>, the rest to <see cref="PayrollResultType.Archive"/>.
    ///
    /// <para>Legacy runs this on the Regular arm only. Preserved.</para>
    /// </summary>
    private async Task ApplyPayrollResultTypeAgeingAsync(int employeeId, DateTime forPeriod, DateTime payInDate)
    {
        var others = _salaryRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.ForPeriod == forPeriod && r.InPeriod != payInDate)
            .OrderByDescending(r => r.InPeriod)
            .ToList();

        for (var i = 0; i < others.Count; i++)
        {
            others[i].PayrollResultType = i == 0 ? PayrollResultType.Last : PayrollResultType.Archive;
            await _salaryRegisterRepository.UpdateAsync(others[i]);
        }
    }

    /// <summary>
    /// Reduces a <see cref="SalaryRegister"/> to the target-agnostic <see cref="PayrollRegisterRef"/> every
    /// wage rule works against. <paramref name="target"/> comes straight from
    /// <see cref="PayrollEngineRunInput.RegisterTarget"/> and is threaded through unchanged -- that is the
    /// whole of what wave P7 (simulation) needs from this engine.
    /// </summary>
    private static PayrollRegisterRef BuildRegisterRef(SalaryRegister register, PayrollRegisterTarget target)
    {
        if (register == null)
        {
            return null;
        }

        return new PayrollRegisterRef
        {
            RegisterId = register.Id,
            Target = target,
            ForPeriod = register.ForPeriod,
            InPeriod = register.InPeriod,
            PayrollRunType = register.PayrollRunType
        };
    }

    #endregion
}
