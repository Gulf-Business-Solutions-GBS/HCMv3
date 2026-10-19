using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Services;
using HCMv3.PayrollManagement;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Legacy <c>HCM10.Module\BusinessObjects\PersonnelAdministration\EmployeeRejoin.cs</c>: <c>PerformValidations</c>
/// (line 221), <c>ValidatePreviousEntry</c> (329), <c>SaveEmployeeInformation</c> (451) and
/// <c>UpdatePreviousValues</c> (769), ported branch for branch. Design note:
/// <c>docs/design/legacy-gap-batch8-round-b.md</c>, section 2, every recommended answer accepted.
///
/// <para><b>Keep the code</b> (<see cref="EmployeeRejoin.CreateNewEmployeeCode"/> off): the employee is made
/// Active again, and each balance that is NOT carried forward is zeroed the day before the rejoin -- leave by an
/// absence adjustment, indemnity by a gratuity record with "reset", tickets by a ticket adjustment, loans by an
/// external recovery of each open loan.</para>
///
/// <para><b>New code</b>: a new, Active employee is created from the old one (legacy's field list), joining on the
/// rejoin date, with <see cref="Employee.PreviousEmpCode"/> = the old code and the next code of the number series.
/// Each balance that IS carried forward is opened on the new employee: a leave Time Transfer, an indemnity quota
/// and a ticket quota whose deductions start 9 months after joining (legacy's Kuwait rule), and one loan per loan
/// type merging the old open loans, recovered over 2 years. The old employee is not touched.</para>
///
/// <para><b>Deliberate differences from legacy</b>, each accepted by the product owner or forced by HCMv3's
/// schema:</para>
/// <list type="bullet">
/// <item><description><b>The "zero ticket balance" adjustment uses the TICKET balance.</b> Legacy (line 643) wrote
/// the indemnity quota's balance. Fixed on the owner's decision.</description></item>
/// <item><description><b>The new employee's Arabic full name is built from the Arabic name parts.</b> Legacy
/// assigned the English full name (line 472); its <c>Employee.OnSaving</c> then rebuilt both names from the parts,
/// so this matches what legacy actually stored.</description></item>
/// <item><description><b>The new employee takes the old one's Employment Type</b>, so the code comes from the right
/// number series. Legacy left it at the default (owner's decision).</description></item>
/// <item><description><b>The new ticket quota names ticket type "1".</b> Legacy left its ticket type blank, which
/// HCMv3's schema does not allow.</description></item>
/// <item><description><b>A missing annual leave type ("1") skips the leave part</b>; legacy threw a null reference
/// and refused the whole save. A missing Time Transfer Type "1" when leave IS carried forward refuses the save
/// with a message, because the transfer cannot be stored without it (legacy stored it typeless).</description></item>
/// <item><description><b>Loan installment schedules are not carried forward.</b> Legacy's
/// <c>EmployeeLoanInstallment</c> (added 16/09/2026) has no HCMv3 counterpart; the merged loan recovers the summed
/// <see cref="EmployeeLoan.FixedAmount"/>, which is what legacy's <c>GetInstallmentAmount</c> falls back to for
/// a loan without installments. Legacy's <c>CalculateRecoveryDate</c> is not ported either (as on the loan
/// screen), so the end date stays joining + 2 years as the rejoin code sets it.</description></item>
/// <item><description><b>Loan balances are refreshed after each recovery</b> written or undone, as
/// <c>FullAndFinalExitService</c> does, because HCMv3 stores <see cref="EmployeeLoan.BalanceDueAmount"/> where
/// legacy computed it.</description></item>
/// </list>
///
/// <para><b>Legacy behaviour kept and pinned by tests:</b> the new employee's probation is zero days, ending on the
/// rejoin date (legacy set <c>ProbationEndDate</c> from the old probation days, but its property setter replaced it
/// with <c>JoiningDate + ProbationDays</c>, and <c>ProbationDays</c> was not copied); the leave, indemnity and
/// ticket balances come from the FIRST quota row only; the zeroing records are matched for undo by date and type,
/// not by comment (except loans); the loan zeroing is dated each loan's end date; the login is not touched; the
/// zeroing records are not system-generated, so they meet the payroll lock like any other change.</para>
///
/// <para>Synchronous LINQ because HCMv3.Core does not use the EF Core async extensions (see
/// <c>EmployeeAbsenceCalculationService</c>). Messages are legacy's own text, as in
/// <see cref="FullAndFinalSettlementService"/>.</para>
/// </summary>
[UnitOfWork]
public class EmployeeRejoinService : HCMv3DomainServiceBase, IEmployeeRejoinService
{
    /// <summary>Legacy's comment on the leave, indemnity and ticket zeroing records.</summary>
    public const string NilQuotaComment = "Employee Rejoined with Nill Quota Balance";

    /// <summary>Legacy's comment on the loan zeroing recoveries -- also how their undo finds them.</summary>
    public const string NilLoanComment = "Employee Rejoined with Nill Loan Balance";

    /// <summary>Annual leave absence type, own ticket type and Time Transfer Type: legacy's hard-coded code "1".</summary>
    public const string CodeOne = "1";

    /// <summary>Legacy: deductions from a carried-forward quota start 9 months after joining ("as per kuwait labour law").</summary>
    public const int DeductionDelayMonths = 9;

    /// <summary>Legacy: a merged loan runs for 2 years from rejoining.</summary>
    public const int MergedLoanYears = 2;

    private static readonly DateTime OpenEnded = new DateTime(9999, 12, 31);

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeRejoin> _rejoinRepository;
    private readonly IRepository<EmployeeSeparation> _separationRepository;
    private readonly IRepository<EmployeeFnFPayment> _fnfPaymentRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<EmployeeAbsenceQuota> _absenceQuotaRepository;
    private readonly IRepository<EmployeeAbsAdjustment> _absAdjustmentRepository;
    private readonly IRepository<TimeTransfer> _timeTransferRepository;
    private readonly IRepository<TimeTransferType> _timeTransferTypeRepository;
    private readonly IRepository<EmployeeGratuityQuota> _gratuityQuotaRepository;
    private readonly IRepository<EmployeeGratuity> _gratuityRepository;
    private readonly IRepository<TicketType> _ticketTypeRepository;
    private readonly IRepository<EmployeeTicketQuota> _ticketQuotaRepository;
    private readonly IRepository<EmployeeTktAdjustment> _tktAdjustmentRepository;
    private readonly IRepository<EmployeeLoan> _loanRepository;
    private readonly IRepository<EmployeeLoanRecovery> _loanRecoveryRepository;
    private readonly IRepository<LoanType> _loanTypeRepository;
    private readonly ISysSettingsAccessor _settings;
    private readonly IEmployeeCodeGenerator _employeeCodeGenerator;
    private readonly IEmployeeValidator _employeeValidator;
    private readonly ICurrencyService _currencyService;
    private readonly HCMv3.Licensing.IActiveEmployeeLimitChecker _activeEmployeeLimitChecker;

    public EmployeeRejoinService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeRejoin> rejoinRepository,
        IRepository<EmployeeSeparation> separationRepository,
        IRepository<EmployeeFnFPayment> fnfPaymentRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<EmployeeAbsenceQuota> absenceQuotaRepository,
        IRepository<EmployeeAbsAdjustment> absAdjustmentRepository,
        IRepository<TimeTransfer> timeTransferRepository,
        IRepository<TimeTransferType> timeTransferTypeRepository,
        IRepository<EmployeeGratuityQuota> gratuityQuotaRepository,
        IRepository<EmployeeGratuity> gratuityRepository,
        IRepository<TicketType> ticketTypeRepository,
        IRepository<EmployeeTicketQuota> ticketQuotaRepository,
        IRepository<EmployeeTktAdjustment> tktAdjustmentRepository,
        IRepository<EmployeeLoan> loanRepository,
        IRepository<EmployeeLoanRecovery> loanRecoveryRepository,
        IRepository<LoanType> loanTypeRepository,
        ISysSettingsAccessor settings,
        IEmployeeCodeGenerator employeeCodeGenerator,
        IEmployeeValidator employeeValidator,
        ICurrencyService currencyService,
        HCMv3.Licensing.IActiveEmployeeLimitChecker activeEmployeeLimitChecker)
    {
        _employeeRepository = employeeRepository;
        _rejoinRepository = rejoinRepository;
        _separationRepository = separationRepository;
        _fnfPaymentRepository = fnfPaymentRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _absenceQuotaRepository = absenceQuotaRepository;
        _absAdjustmentRepository = absAdjustmentRepository;
        _timeTransferRepository = timeTransferRepository;
        _timeTransferTypeRepository = timeTransferTypeRepository;
        _gratuityQuotaRepository = gratuityQuotaRepository;
        _gratuityRepository = gratuityRepository;
        _ticketTypeRepository = ticketTypeRepository;
        _ticketQuotaRepository = ticketQuotaRepository;
        _tktAdjustmentRepository = tktAdjustmentRepository;
        _loanRepository = loanRepository;
        _loanRecoveryRepository = loanRecoveryRepository;
        _loanTypeRepository = loanTypeRepository;
        _settings = settings;
        _employeeCodeGenerator = employeeCodeGenerator;
        _employeeValidator = employeeValidator;
        _currencyService = currencyService;
        _activeEmployeeLimitChecker = activeEmployeeLimitChecker;
    }

    #region << Validation >>

    /// <inheritdoc/>
    public async Task ValidateSaveAsync(EmployeeRejoinValues current, EmployeeRejoinValues previous, int? rejoinId)
    {
        // Legacy 223-227.
        var employee = current.EmployeeId == 0 ? null : FindEmployee(current.EmployeeId);
        if (employee == null)
        {
            throw new UserFriendlyException("Employee Cannot be blank.");
        }

        if (current.RejoinDate == default)
        {
            throw new UserFriendlyException("Start Date cannot be null.");
        }

        await ValidatePreviousEntryAsync(previous, rejoinId);

        // Legacy 231-327: the new values against the employee's later history.
        await ValidateHistoryAsync(employee, current.RejoinDate.Date, rejoinId);
    }

    /// <inheritdoc/>
    public async Task ValidateDeleteAsync(EmployeeRejoinValues previous, int rejoinId)
    {
        await ValidatePreviousEntryAsync(previous, rejoinId);
    }

    /// <summary>
    /// Legacy <c>ValidatePreviousEntry</c> (line 329): the entry as it was loaded must still be changeable. It is
    /// not once it has created a new employee, or once a later exit, F&amp;F, payroll run or rejoin exists.
    /// </summary>
    private async Task ValidatePreviousEntryAsync(EmployeeRejoinValues previous, int? rejoinId)
    {
        if (previous == null)
        {
            return;
        }

        var previousEmployee = FindEmployee(previous.EmployeeId);
        if (previousEmployee == null)
        {
            return;
        }

        // Legacy 338-351.
        if (previous.CreateNewEmployeeCode)
        {
            var newEmployee = _employeeRepository.GetAll()
                .Where(e => e.PreviousEmpCode == previousEmployee.Code)
                .OrderBy(e => e.Id)
                .FirstOrDefault();
            if (newEmployee != null)
            {
                throw new UserFriendlyException(
                    "The New employee created from this rejoining entry. You cannot add, edit, or delete the rejoin entry." + Environment.NewLine
                    + Environment.NewLine
                    + "Employee : " + CodeWithFullNameEn(previousEmployee) + Environment.NewLine
                    + "New Employee : " + CodeWithFullNameEn(newEmployee) + Environment.NewLine);
            }
        }

        // Legacy 354-446: the same four checks as the current entry, against the previous employee and date.
        await ValidateHistoryAsync(previousEmployee, previous.RejoinDate.Date, rejoinId);
    }

    /// <summary>
    /// The four checks legacy repeats in <c>PerformValidations</c> and <c>ValidatePreviousEntry</c>: a later exit,
    /// F&amp;F, regular payroll run or rejoin means this rejoin can no longer be added, changed or deleted.
    /// </summary>
    private async Task ValidateHistoryAsync(Employee employee, DateTime rejoinDate, int? rejoinId)
    {
        var timeSettings = await _settings.GetTimeSettingsAsync();
        var earliestEvaluationDate = timeSettings?.EarliestEvaluationDate ?? default(DateTime);

        // Legacy 237-248: the later of joining and the earliest evaluation date, moved to the EARLIEST other
        // rejoin from there on (legacy orders ascending here, unlike the F&F's descending).
        var startRejoin = employee.JoiningDate > earliestEvaluationDate ? employee.JoiningDate : earliestEvaluationDate;

        var firstOtherRejoin = _rejoinRepository.GetAll()
            .Where(r => (rejoinId == null || r.Id != rejoinId.Value)
                        && r.EmployeeId == employee.Id
                        && r.RejoinDate >= startRejoin)
            .OrderBy(r => r.RejoinDate)
            .FirstOrDefault();
        if (firstOtherRejoin != null)
        {
            startRejoin = firstOtherRejoin.RejoinDate;
        }

        // Legacy 251-267. No revoke filter, as legacy.
        var exit = _separationRepository.GetAll()
            .Where(r => r.EmployeeId == employee.Id
                        && r.SubmissionDate >= startRejoin
                        && r.SubmissionDate > rejoinDate)
            .OrderBy(r => r.Id)
            .FirstOrDefault();
        if (exit != null)
        {
            throw new UserFriendlyException(
                "The Exit has already been made for this employee. You cannot add, edit, or delete the rejoin entry." + Environment.NewLine
                + Environment.NewLine
                + "Employee : " + CodeWithFullNameEn(employee) + Environment.NewLine
                + "Submission Date : " + exit.SubmissionDate.ToString("dd/MM/yyyy") + Environment.NewLine
                + "Please check startdate." + Environment.NewLine);
        }

        // Legacy 269-285.
        var fnf = _fnfPaymentRepository.GetAll()
            .Where(r => r.EmployeeId == employee.Id
                        && r.CompensationDate >= startRejoin
                        && r.CompensationDate > rejoinDate)
            .OrderBy(r => r.Id)
            .FirstOrDefault();
        if (fnf != null)
        {
            throw new UserFriendlyException(
                "The Fnf Payment has already been made for this employee. You cannot add, edit, or delete the rejoin entry." + Environment.NewLine
                + Environment.NewLine
                + "Employee : " + CodeWithFullNameEn(employee) + Environment.NewLine
                + "Compensation Date : " + fnf.CompensationDate.ToString("dd/MM/yyyy") + Environment.NewLine
                + "Please check startdate." + Environment.NewLine);
        }

        // Legacy 287-305: a regular run (ForPeriod == InPeriod) for a period from the rejoin date on.
        var register = _salaryRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employee.Id
                        && r.ForPeriod == r.InPeriod
                        && r.ForPeriod >= rejoinDate)
            .OrderBy(r => r.Id)
            .FirstOrDefault();
        if (register != null)
        {
            throw new UserFriendlyException(
                "The salary register has already been processed for this employee. Therefore, you cannot add, edit, or delete the rejoin entry." + Environment.NewLine
                + Environment.NewLine
                + "Employee : " + CodeWithFullNameEn(employee) + Environment.NewLine
                + "Run Type : " + register.PayrollRunType + Environment.NewLine
                + "Salary ForPeriod: " + register.ForPeriod.ToString("dd/MM/yyyy") + Environment.NewLine
                + "Please check startdate." + Environment.NewLine);
        }

        // Legacy 307-325.
        var laterRejoin = _rejoinRepository.GetAll()
            .Where(r => (rejoinId == null || r.Id != rejoinId.Value)
                        && r.EmployeeId == employee.Id
                        && r.RejoinDate >= startRejoin
                        && r.RejoinDate > rejoinDate)
            .OrderBy(r => r.RejoinDate)
            .FirstOrDefault();
        if (laterRejoin != null)
        {
            throw new UserFriendlyException(
                "The Rejoin has already been made for this employee. You cannot add, edit, or delete the rejoin entry." + Environment.NewLine
                + Environment.NewLine
                + "Employee : " + CodeWithFullNameEn(employee) + Environment.NewLine
                + "Rejoin Date : " + laterRejoin.RejoinDate.ToString("dd/MM/yyyy") + Environment.NewLine
                + "Please check startdate." + Environment.NewLine);
        }
    }

    #endregion

    #region << Save and undo >>

    /// <inheritdoc/>
    public async Task<int?> ApplyAsync(EmployeeRejoinValues current, EmployeeRejoinValues previous)
    {
        // Legacy OnSaving: UpdatePreviousValues, then SaveEmployeeInformation.
        if (previous != null)
        {
            await UndoPreviousValuesAsync(previous);

            // The undo SOFT-deletes. Unsaved, the reads below would still see those rows (the trap
            // FullAndFinalSettlementService.ApplyAsync documents).
            await CurrentUnitOfWork.SaveChangesAsync();
        }

        return await SaveEmployeeInformationAsync(current);
    }

    /// <inheritdoc/>
    public async Task UndoAsync(EmployeeRejoinValues previous)
    {
        await UndoPreviousValuesAsync(previous);
        await CurrentUnitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Legacy <c>SaveEmployeeInformation</c> (line 451). Every balance is read from the OLD employee.
    /// </summary>
    private async Task<int?> SaveEmployeeInformationAsync(EmployeeRejoinValues current)
    {
        var oldEmployee = FindEmployee(current.EmployeeId)
                          ?? throw new UserFriendlyException("Employee Cannot be blank.");
        var rejoinDate = current.RejoinDate.Date;
        var dayBefore = rejoinDate.AddDays(-1);

        // Refused before anything is written, rather than half-way through.
        var leaveTransferType = LeaveTransferTypeIfNeeded(current, oldEmployee);

        Employee employee;
        if (current.CreateNewEmployeeCode)
        {
            employee = await CreateNewEmployeeAsync(oldEmployee, rejoinDate);
        }
        else
        {
            employee = oldEmployee;
            // Reactivating the same record counts against the subscription's active-employee limit.
            await _activeEmployeeLimitChecker.EnsureCanActivateAsync(employee.TenantId, employee.Id);
            employee.Status = EmployeeStatus.Active;
            await _employeeRepository.UpdateAsync(employee);
        }

        await LeaveAsync(current, oldEmployee, employee, rejoinDate, dayBefore, leaveTransferType);
        await IndemnityAsync(current, oldEmployee, employee, dayBefore);
        await TicketAsync(current, oldEmployee, employee, dayBefore);
        await LoansAsync(current, oldEmployee, employee);

        return current.CreateNewEmployeeCode ? employee.Id : null;
    }

    /// <summary>
    /// Legacy 460-520: the new employee. Photo, passport, access ID, login, org assignment, salary and bank are NOT
    /// copied (legacy, and design decision 5); HR enters them as for any new joiner.
    /// </summary>
    private async Task<Employee> CreateNewEmployeeAsync(Employee old, DateTime rejoinDate)
    {
        var employee = new Employee
        {
            TenantId = old.TenantId,
            FirstNameEn = old.FirstNameEn,
            SecondNameEn = old.SecondNameEn,
            ThirdNameEn = old.ThirdNameEn,
            LastNameEn = old.LastNameEn,
            FirstNameAr = old.FirstNameAr,
            SecondNameAr = old.SecondNameAr,
            ThirdNameAr = old.ThirdNameAr,
            LastNameAr = old.LastNameAr,
            FullNameEn = old.FullNameEn,
            // Legacy line 472 copied the ENGLISH full name; fixed (decision 1). ApplyFullNames below rebuilds both
            // from the parts anyway, as legacy's Employee.OnSaving did.
            FullNameAr = old.FullNameAr,
            TitleId = old.TitleId,
            Status = EmployeeStatus.Active,
            BirthDate = old.BirthDate,
            JoiningDate = rejoinDate,
            // Legacy's effective result (see the class remarks): ProbationDays not copied, so the end date is the
            // joining date.
            ProbationDays = 0,
            ProbationEndDate = rejoinDate,
            NationalityId = old.NationalityId,
            ReligionId = old.ReligionId,
            Gender = old.Gender,
            MaritalStatusId = old.MaritalStatusId,
            CountryId = old.CountryId,
            Telephone = old.Telephone,
            Mobile = old.Mobile,
            CurrentAddress = old.CurrentAddress,
            PermanentAddress = old.PermanentAddress,
            QualificationId = old.QualificationId,
            BloodGroup = old.BloodGroup,
            CivilID = old.CivilID,
            Email = old.Email,
            BirthPlaceId = old.BirthPlaceId,
            PreviousEmpCode = old.Code,
            // Not copied by legacy; copied on decision 3 so the code comes from the right number series.
            EmploymentType = old.EmploymentType
        };

        employee.ApplyFullNames();

        // Legacy Employee.OnSaving generated the code whenever it was blank, whatever DisableAutoEmpcode says (that
        // setting only unlocks the code editor), as the recruitment Joined hand-off does.
        _employeeCodeGenerator.AssignCodeIfEmpty(employee);
        _employeeValidator.Validate(employee);

        employee.Id = await _employeeRepository.InsertAndGetIdAsync(employee);
        return employee;
    }

    /// <summary>
    /// The Time Transfer Type "1" a carried-forward leave balance is booked with, when one will be booked: a new
    /// code, leave ticked, and an annual leave quota on the old employee. Refuses the save when it is missing.
    /// </summary>
    private TimeTransferType LeaveTransferTypeIfNeeded(EmployeeRejoinValues current, Employee old)
    {
        if (!current.CreateNewEmployeeCode || !current.CarryForwardLeaveBalance || OldLeaveQuota(old, out _) == null)
        {
            return null;
        }

        return _timeTransferTypeRepository.GetAll().FirstOrDefault(r => r.Code == CodeOne)
               ?? throw new UserFriendlyException(
                   "Time Transfer Type with code '1' not found. It is needed to carry the leave balance forward.");
    }

    /// <summary>Legacy 532-534: the FIRST annual leave ("1") quota of the old employee.</summary>
    private EmployeeAbsenceQuota OldLeaveQuota(Employee old, out AbsenceType annualLeave)
    {
        annualLeave = _absenceTypeRepository.GetAll().FirstOrDefault(r => r.Code == CodeOne);
        if (annualLeave == null)
        {
            return null;
        }

        var annualLeaveId = annualLeave.Id;
        return _absenceQuotaRepository.GetAll()
            .Where(r => r.EmployeeId == old.Id && r.AbsenceTypeId == annualLeaveId)
            .OrderBy(r => r.Id)
            .FirstOrDefault();
    }

    /// <summary>Legacy 530-575.</summary>
    private async Task LeaveAsync(
        EmployeeRejoinValues current, Employee old, Employee employee, DateTime rejoinDate, DateTime dayBefore,
        TimeTransferType transferType)
    {
        var quota = OldLeaveQuota(old, out var annualLeave);
        if (quota == null)
        {
            return;
        }

        var balance = AbsenceQuotaBalance(quota);

        if (!current.CreateNewEmployeeCode && !current.CarryForwardLeaveBalance)
        {
            await _absAdjustmentRepository.InsertAsync(new EmployeeAbsAdjustment
            {
                TenantId = old.TenantId,
                EmployeeId = old.Id,
                AbsenceTypeId = annualLeave.Id,
                AdjustmentType = AdjustmentType.Adjust,
                EffectiveDate = dayBefore,
                QuotaAdjusted = balance,
                Comments = NilQuotaComment
            });
        }
        else if (current.CreateNewEmployeeCode && current.CarryForwardLeaveBalance)
        {
            await _timeTransferRepository.InsertAsync(new TimeTransfer
            {
                TenantId = employee.TenantId,
                EmployeeId = employee.Id,
                TransferDate = rejoinDate,
                TransferCount = balance,
                TransferTypeId = transferType.Id
            });
        }
    }

    /// <summary>Legacy 577-620.</summary>
    private async Task IndemnityAsync(EmployeeRejoinValues current, Employee old, Employee employee, DateTime dayBefore)
    {
        var quota = _gratuityQuotaRepository.GetAll()
            .Where(r => r.EmployeeId == old.Id)
            .OrderBy(r => r.Id)
            .FirstOrDefault();
        if (quota == null)
        {
            return;
        }

        var balance = GratuityQuotaBalance(quota);

        if (!current.CreateNewEmployeeCode && !current.CarryForwardIndemnityBalance)
        {
            await _gratuityRepository.InsertAsync(new EmployeeGratuity
            {
                TenantId = old.TenantId,
                EmployeeId = old.Id,
                GratuityPaidDays = balance,
                GratuityPaidDate = dayBefore,
                ResetGratuityCalc = true,
                Comments = NilQuotaComment
            });
        }
        else if (current.CreateNewEmployeeCode && current.CarryForwardIndemnityBalance)
        {
            var from = employee.JoiningDate.Date;
            await _gratuityQuotaRepository.InsertAsync(new EmployeeGratuityQuota
            {
                TenantId = employee.TenantId,
                EmployeeId = employee.Id,
                QuotaAccrued = balance,
                EntitlementFrom = from,
                EntitlementTo = OpenEnded,
                DeductionFrom = from.AddMonths(DeductionDelayMonths),
                DeductionTo = OpenEnded
            });
        }
    }

    /// <summary>Legacy 622-668.</summary>
    private async Task TicketAsync(EmployeeRejoinValues current, Employee old, Employee employee, DateTime dayBefore)
    {
        // Legacy compared r.TicketType == null when type "1" was missing, which no HCMv3 quota can match.
        var ticketType = _ticketTypeRepository.GetAll().FirstOrDefault(r => r.Code == CodeOne);
        if (ticketType == null)
        {
            return;
        }

        var quota = _ticketQuotaRepository.GetAll()
            .Where(r => r.EmployeeId == old.Id && r.TicketTypeId == ticketType.Id)
            .OrderBy(r => r.Id)
            .FirstOrDefault();
        if (quota == null)
        {
            return;
        }

        var balance = TicketQuotaBalance(quota);

        if (!current.CreateNewEmployeeCode && !current.CarryForwardTicketBalance)
        {
            await _tktAdjustmentRepository.InsertAsync(new EmployeeTktAdjustment
            {
                TenantId = old.TenantId,
                EmployeeId = old.Id,
                TicketTypeId = ticketType.Id,
                AdjustmentType = AdjustmentType.Adjust,
                EffectiveDate = dayBefore,
                // Legacy line 643 used the INDEMNITY quota's balance here; fixed to the ticket balance (decision 2).
                QuotaAdjusted = balance,
                TicketPaymentType = TicketPaymentType.PayInAdvance,
                Comments = NilQuotaComment
            });
        }
        else if (current.CreateNewEmployeeCode && current.CarryForwardTicketBalance)
        {
            var from = employee.JoiningDate.Date;
            await _ticketQuotaRepository.InsertAsync(new EmployeeTicketQuota
            {
                TenantId = employee.TenantId,
                EmployeeId = employee.Id,
                // Legacy left the ticket type blank; HCMv3 requires one.
                TicketTypeId = ticketType.Id,
                QuotaAccrued = balance,
                EntitlementFrom = from,
                EntitlementTo = OpenEnded,
                DeductionFrom = from.AddMonths(DeductionDelayMonths),
                DeductionTo = OpenEnded
            });
        }
    }

    /// <summary>Legacy 670-762: the old employee's loans with a balance still due.</summary>
    private async Task LoansAsync(EmployeeRejoinValues current, Employee old, Employee employee)
    {
        var openLoans = _loanRepository.GetAll()
            .Where(r => r.EmployeeId == old.Id && r.BalanceDueAmount > 0)
            .OrderBy(r => r.Id)
            .ToList();
        if (openLoans.Count == 0)
        {
            return;
        }

        if (!current.CreateNewEmployeeCode && !current.CarryForwardLoanBalance)
        {
            // Legacy 684-693: each loan is recovered externally on ITS OWN end date.
            foreach (var loan in openLoans)
            {
                await _loanRecoveryRepository.InsertAsync(new EmployeeLoanRecovery
                {
                    TenantId = loan.TenantId,
                    EmployeeLoanId = loan.Id,
                    LoanRecoveryMethod = LoanRecoveryMethod.RecoveryExternal,
                    RecoveryAmount = loan.BalanceDueAmount,
                    RecoveryDate = loan.EndDate,
                    Comments = NilLoanComment
                });
            }

            await CurrentUnitOfWork.SaveChangesAsync();
            await RefreshStoredBalancesAsync(openLoans.Select(l => l.Id));
        }
        else if (current.CreateNewEmployeeCode && current.CarryForwardLoanBalance)
        {
            // Legacy 701-754: one new loan per loan type, combining the balances and the instalments.
            var joining = employee.JoiningDate;
            foreach (var loanType in _loanTypeRepository.GetAll().OrderBy(r => r.Id).ToList())
            {
                var ofType = openLoans.Where(r => r.LoanTypeId == loanType.Id).ToList();
                if (ofType.Count == 0)
                {
                    continue;
                }

                var balanceDue = ofType.Sum(r => r.BalanceDueAmount);

                await _loanRepository.InsertAsync(new EmployeeLoan
                {
                    TenantId = employee.TenantId,
                    EmployeeId = employee.Id,
                    LoanTypeId = loanType.Id,
                    StartDate = joining,
                    EndDate = joining.AddYears(MergedLoanYears),
                    RecoveryStartDate = joining,
                    FixedAmount = ofType.Sum(r => r.FixedAmount),
                    ActualAmount = balanceDue,
                    DueAmount = balanceDue,
                    BalanceDueAmount = balanceDue,
                    // Legacy EmployeeLoan.AfterConstruction default.
                    IncludeInLoanPay = true
                });
            }
        }
    }

    /// <summary>
    /// Legacy <c>UpdatePreviousValues</c> (line 769): on an edit or delete of an entry that KEPT the code, the
    /// employee goes back to Inactive and the zeroing records that save wrote are deleted. An entry that created a
    /// new employee undoes nothing (and <c>ValidatePreviousEntry</c> has refused it once that employee exists).
    /// </summary>
    private async Task UndoPreviousValuesAsync(EmployeeRejoinValues previous)
    {
        if (previous == null)
        {
            return;
        }

        var employee = FindEmployee(previous.EmployeeId);
        if (employee == null || previous.CreateNewEmployeeCode)
        {
            return;
        }

        employee.Status = EmployeeStatus.Inactive;
        await _employeeRepository.UpdateAsync(employee);

        var dayBefore = previous.RejoinDate.Date.AddDays(-1);

        // Legacy 782-806.
        if (!previous.CarryForwardLeaveBalance)
        {
            var annualLeave = _absenceTypeRepository.GetAll().FirstOrDefault(r => r.Code == CodeOne);
            if (annualLeave != null
                && _absenceQuotaRepository.GetAll().Any(r => r.EmployeeId == employee.Id && r.AbsenceTypeId == annualLeave.Id))
            {
                var adjustment = _absAdjustmentRepository.GetAll()
                    .Where(r => r.EmployeeId == employee.Id
                                && r.AbsenceTypeId == annualLeave.Id
                                && r.AdjustmentType == AdjustmentType.Adjust
                                && r.EffectiveDate == dayBefore)
                    .OrderBy(r => r.Id)
                    .FirstOrDefault();
                if (adjustment != null)
                {
                    await _absAdjustmentRepository.DeleteAsync(adjustment);
                }
            }
        }

        // Legacy 808-823.
        if (!previous.CarryForwardIndemnityBalance
            && _gratuityQuotaRepository.GetAll().Any(r => r.EmployeeId == employee.Id))
        {
            var gratuity = _gratuityRepository.GetAll()
                .Where(r => r.EmployeeId == employee.Id && r.GratuityPaidDate == dayBefore)
                .OrderBy(r => r.Id)
                .FirstOrDefault();
            if (gratuity != null)
            {
                await _gratuityRepository.DeleteAsync(gratuity);
            }
        }

        // Legacy 825-850.
        if (!previous.CarryForwardTicketBalance)
        {
            var ticketType = _ticketTypeRepository.GetAll().FirstOrDefault(r => r.Code == CodeOne);
            if (ticketType != null
                && _ticketQuotaRepository.GetAll().Any(r => r.EmployeeId == employee.Id && r.TicketTypeId == ticketType.Id))
            {
                var adjustment = _tktAdjustmentRepository.GetAll()
                    .Where(r => r.EmployeeId == employee.Id
                                && r.TicketTypeId == ticketType.Id
                                && r.AdjustmentType == AdjustmentType.Adjust
                                && r.EffectiveDate == dayBefore
                                && r.TicketPaymentType == TicketPaymentType.PayInAdvance)
                    .OrderBy(r => r.Id)
                    .FirstOrDefault();
                if (adjustment != null)
                {
                    await _tktAdjustmentRepository.DeleteAsync(adjustment);
                }
            }
        }

        // Legacy 852-867.
        if (!previous.CarryForwardLoanBalance)
        {
            var recoveries = _loanRecoveryRepository.GetAll()
                .Where(r => r.EmployeeLoan.EmployeeId == employee.Id
                            && r.LoanRecoveryMethod == LoanRecoveryMethod.RecoveryExternal
                            && r.RecoveryDate == r.EmployeeLoan.EndDate
                            && r.Comments == NilLoanComment)
                .ToList();

            if (recoveries.Count > 0)
            {
                foreach (var recovery in recoveries)
                {
                    await _loanRecoveryRepository.DeleteAsync(recovery);
                }

                await CurrentUnitOfWork.SaveChangesAsync();
                await RefreshStoredBalancesAsync(recoveries.Select(r => r.EmployeeLoanId).Distinct());
            }
        }
    }

    #endregion

    #region << Helpers >>

    private Employee FindEmployee(int employeeId)
    {
        return _employeeRepository.GetAll().FirstOrDefault(e => e.Id == employeeId);
    }

    /// <summary>
    /// Legacy <c>EmployeeLoan.BalanceDueAmount</c> = <c>Rounding(emp, DueAmount - Σ RecoveryAmount)</c>, stored, as
    /// <c>FullAndFinalExitService</c> keeps it.
    /// </summary>
    private async Task RefreshStoredBalancesAsync(IEnumerable<int> loanIds)
    {
        foreach (var loanId in loanIds.ToList())
        {
            var loan = _loanRepository.GetAll().FirstOrDefault(r => r.Id == loanId);
            if (loan == null)
            {
                continue;
            }

            var totalPaid = _loanRecoveryRepository.GetAll()
                .Where(r => r.EmployeeLoanId == loan.Id)
                .Select(r => r.RecoveryAmount)
                .ToList()
                .Sum();

            loan.BalanceDueAmount = await _currencyService.RoundAsync(loan.EmployeeId, loan.DueAmount - totalPaid);
            await _loanRepository.UpdateAsync(loan);
        }
    }

    /// <summary>Legacy <c>EmployeeAbsenceQuota.QuotaBalance</c>, rounded to 2.</summary>
    public static double AbsenceQuotaBalance(EmployeeAbsenceQuota q)
        => Math.Round(q.QuotaAccrued - (q.QuotaUsed + q.QuotaAdjusted + q.QuotaForfeited), 2);

    /// <summary>Legacy <c>EmployeeGratuityQuota.QuotaBalance</c>, rounded to 3.</summary>
    public static double GratuityQuotaBalance(EmployeeGratuityQuota q)
        => Math.Round(q.QuotaAccrued - (q.QuotaUsed + q.QuotaAdjusted), 3);

    /// <summary>Legacy <c>EmployeeTicketQuota.QuotaBalance</c>, rounded to 3.</summary>
    public static double TicketQuotaBalance(EmployeeTicketQuota q)
        => Math.Round(q.QuotaAccrued - (q.QuotaUsed + q.QuotaAdjusted), 3);

    private static string CodeWithFullNameEn(Employee employee)
        => string.IsNullOrWhiteSpace(employee?.FullNameEn) ? employee?.Code : employee.Code + " " + employee.FullNameEn;

    #endregion
}
