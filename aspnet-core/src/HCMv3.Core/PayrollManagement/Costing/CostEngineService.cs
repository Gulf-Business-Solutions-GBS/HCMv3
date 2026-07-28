using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.ProjectManagement;

namespace HCMv3.PayrollManagement.Costing;

/// <inheritdoc cref="ICostEngineService"/>
[UnitOfWork]
public class CostEngineService : HCMv3DomainServiceBase, ICostEngineService
{
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<CostRegister> _costRegisterRepository;
    private readonly IRepository<CostRegisterDetail> _costRegisterDetailRepository;
    private readonly IRepository<EmployeeWageAccount> _employeeWageAccountRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<ProjectLog> _projectLogRepository;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;

    public CostEngineService(
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<CostRegister> costRegisterRepository,
        IRepository<CostRegisterDetail> costRegisterDetailRepository,
        IRepository<EmployeeWageAccount> employeeWageAccountRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<ProjectLog> projectLogRepository,
        IEmployeePayrollAreaResolver payrollAreaResolver)
    {
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _costRegisterRepository = costRegisterRepository;
        _costRegisterDetailRepository = costRegisterDetailRepository;
        _employeeWageAccountRepository = employeeWageAccountRepository;
        _wageTypeRepository = wageTypeRepository;
        _employeeRepository = employeeRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _projectLogRepository = projectLogRepository;
        _payrollAreaResolver = payrollAreaResolver;
    }

    public async Task<CostingRunResult> RunAsync(CostingRunInput input)
    {
        var result = new CostingRunResult();

        // Legacy line 442: the period is normalised to the LAST DAY of the month at 00:00:00 before it is
        // used as the CostRegister.Period / SalaryRegister.ForPeriod|InPeriod match key.
        var payForPeriod = NormalizeToEndOfMonth(input.PayInDate);

        var employeeIds = await ResolveEmployeeIdsAsync(input);
        result.TotalEmployees = employeeIds.Count;

        if (!employeeIds.Any())
        {
            return result;
        }

        var employeeCodes = _employeeRepository.GetAll()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.Code })
            .ToList()
            .ToDictionary(e => e.Id, e => e.Code);

        // Legacy resolves the arrear source wage type per row; pre-load the whole (small) wage-type table.
        var wageTypes = _wageTypeRepository.GetAll()
            .Select(w => new WageTypeRow
            {
                Id = w.Id,
                Code = w.Code,
                NameEn = w.NameEn,
                IncludeInFIPosting = w.IncludeInFIPosting,
                IncludeInArrearPay = w.IncludeInArrearPay,
                ArrearSourceWageTypeId = w.ArrearSourceWageTypeId
            })
            .ToList()
            .ToDictionary(w => w.Id, w => w);

        foreach (var employeeId in employeeIds)
        {
            // Legacy lines 468-473: a ProjectLog row with StopFlag aborts the whole run mid-loop.
            if (_projectLogRepository.GetAll().Any(l => l.StopFlag))
            {
                result.Aborted = true;
                break;
            }

            var employeeCode = employeeCodes.TryGetValue(employeeId, out var code) ? code : null;

            try
            {
                await ProcessEmployeeAsync(employeeId, payForPeriod, input.PayrollRunType, wageTypes);

                result.SuccessCount++;
                result.Entries.Add(new CostingRunLogEntry
                {
                    EmployeeId = employeeId,
                    EmployeeCode = employeeCode,
                    Status = 0,
                    Message = SuccessMessage
                });
            }
            catch (Exception ex)
            {
                // Legacy lines 630-654: a per-employee failure is LOGGED and the loop continues; it never
                // aborts the run. Message is truncated to 1000 chars (legacy line 639).
                result.FailedCount++;
                var message = ex.Message ?? "";
                result.Entries.Add(new CostingRunLogEntry
                {
                    EmployeeId = employeeId,
                    EmployeeCode = employeeCode,
                    Status = 1,
                    Message = message.Length > MaxLogMessageLength
                        ? message.Substring(0, MaxLogMessageLength)
                        : message
                });
            }
        }

        return result;
    }

    private async Task ProcessEmployeeAsync(
        int employeeId,
        DateTime payForPeriod,
        PayrollRunType payrollRunType,
        Dictionary<int, WageTypeRow> wageTypes)
    {
        // ---- Delete + rebuild the existing cost register (legacy lines 486-507) --------------------
        var existingRegisters = _costRegisterRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId
                        && r.Period == payForPeriod
                        && r.PayrollRunType == payrollRunType)
            .Select(r => r.Id)
            .ToList();

        if (existingRegisters.Count > 1)
        {
            // Legacy line 494, message preserved in shape.
            throw new UserFriendlyException(
                L("DuplicateCostRegisterFound", payForPeriod.ToString("dd/MM/yyyy")));
        }

        int? costRegisterId = existingRegisters.FirstOrDefault();
        if (costRegisterId == 0)
        {
            costRegisterId = null;
        }

        if (costRegisterId != null)
        {
            var staleDetailIds = _costRegisterDetailRepository.GetAll()
                .Where(d => d.CostRegisterId == costRegisterId.Value)
                .Select(d => d.Id)
                .ToList();

            foreach (var detailId in staleDetailIds)
            {
                await _costRegisterDetailRepository.DeleteAsync(detailId);
            }
        }

        // ---- Locate the salary register (legacy lines 511-514) -------------------------------------
        // Legacy matches ForPeriod == InPeriod == payForPeriod, i.e. only NON-retro results are costed.
        var salaryRegister = _salaryRegisterRepository.GetAll()
            .FirstOrDefault(r => r.EmployeeId == employeeId
                                 && r.ForPeriod == payForPeriod
                                 && r.InPeriod == payForPeriod
                                 && r.PayrollRunType == payrollRunType);

        // Legacy line 515: no salary register simply means nothing to cost -- NOT an error.
        if (salaryRegister == null)
        {
            return;
        }

        if (costRegisterId == null)
        {
            var costRegister = new CostRegister
            {
                EmployeeId = salaryRegister.EmployeeId,
                Period = salaryRegister.InPeriod,
                PayrollRunType = salaryRegister.PayrollRunType
            };

            // InsertAndGetIdAsync (not InsertAsync): the identity is needed immediately for the details.
            costRegisterId = await _costRegisterRepository.InsertAndGetIdAsync(costRegister);
        }

        // ---- Fold arrears onto their source wage type (legacy lines 535-548) -----------------------
        var registerLines = _salaryRegisterDetailRepository.GetAll()
            .Where(d => d.SalaryRegisterId == salaryRegister.Id)
            .Select(d => new { d.WageTypeId, d.Amount })
            .ToList();

        var foldedLines = new List<(int WageTypeId, double Amount)>();
        foreach (var registerLine in registerLines)
        {
            if (!wageTypes.TryGetValue(registerLine.WageTypeId, out var wageType) || !wageType.IncludeInFIPosting)
            {
                continue;
            }

            // Legacy line 539: `IncludeInArrearPay == false ? WageType : WageType.ArrearSourceWageType`.
            // Legacy does not null-guard ArrearSourceWageType; an arrear wage type with no source would
            // NRE there. Here it is skipped instead, which is the closest non-crashing equivalent.
            var effectiveWageTypeId = registerLine.WageTypeId;
            if (wageType.IncludeInArrearPay)
            {
                if (wageType.ArrearSourceWageTypeId == null)
                {
                    continue;
                }

                effectiveWageTypeId = wageType.ArrearSourceWageTypeId.Value;
            }

            foldedLines.Add((effectiveWageTypeId, registerLine.Amount));
        }

        // Legacy lines 543-548: the DISTINCT wage-type list drives the loop; the amount is the SUM over
        // all folded lines of that wage type (line 576), so an arrear line and its base line are costed
        // together as one amount.
        var distinctWageTypeIds = foldedLines.Select(l => l.WageTypeId).Distinct().ToList();

        var currencyId = ResolveEmployeeCurrencyId(employeeId, payForPeriod);

        foreach (var wageTypeId in distinctWageTypeIds)
        {
            // ---- Account/cost-element configuration lookup (legacy lines 553-571) ------------------
            // Legacy's `!(WageType.Code == "9000" && AccountInfo != null)` filter excludes configured Net
            // Pay rows; combined with the FIPosting flag it means Net Pay never gets its own cost detail.
            var wageTypeCode = wageTypes.TryGetValue(wageTypeId, out var wt) ? wt.Code : null;

            var wageAccounts = _employeeWageAccountRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId
                            && r.WageTypeId == wageTypeId
                            && r.StartDate <= payForPeriod
                            && r.EndDate >= payForPeriod)
                .Select(r => new WageAccountRow
                {
                    WageTypeId = r.WageTypeId,
                    AccountInfoId = r.AccountInfoId,
                    CostElement1Id = r.CostElement1Id,
                    CostElement2Id = r.CostElement2Id,
                    CostElement3Id = r.CostElement3Id,
                    CostElement4Id = r.CostElement4Id,
                    CostElement5Id = r.CostElement5Id,
                    CostElement6Id = r.CostElement6Id,
                    CostElement7Id = r.CostElement7Id,
                    CostPercentage = r.CostPercentage
                })
                .ToList();

            if (wageTypeCode == JournalVoucherBuilder.NetPayWageTypeCode)
            {
                wageAccounts.Clear();
            }

            if (!wageAccounts.Any())
            {
                // Legacy line 604: an unconfigured wage type FAILS the employee.
                throw new UserFriendlyException(L("WageTypeNotConfiguredForEmployee",
                    wageTypes.TryGetValue(wageTypeId, out var w) ? w.NameEn : wageTypeId.ToString(),
                    employeeId.ToString(),
                    payForPeriod.ToString("dd/MM/yyyy")));
            }

            var totalAmount = foldedLines.Where(l => l.WageTypeId == wageTypeId).Sum(l => l.Amount);

            foreach (var wageAccount in wageAccounts)
            {
                await _costRegisterDetailRepository.InsertAsync(new CostRegisterDetail
                {
                    CostRegisterId = costRegisterId.Value,
                    WageTypeId = wageAccount.WageTypeId,
                    AccountInfoId = wageAccount.AccountInfoId,
                    CostElement1Id = wageAccount.CostElement1Id,
                    CostElement2Id = wageAccount.CostElement2Id,
                    CostElement3Id = wageAccount.CostElement3Id,
                    CostElement4Id = wageAccount.CostElement4Id,
                    CostElement5Id = wageAccount.CostElement5Id,
                    CostElement6Id = wageAccount.CostElement6Id,
                    CostElement7Id = wageAccount.CostElement7Id,

                    // Legacy line 590, verbatim: NO rounding, NO remainder redistribution.
                    // See ICostEngineService remarks.
                    Amount = (totalAmount * wageAccount.CostPercentage) / 100,

                    CurrencyId = currencyId
                });
            }
        }
    }

    /// <summary>
    /// Legacy <c>GetEmployeeList</c> (<c>uscCostingFile.ascx.cs</c> lines 2461-2488): an explicit employee
    /// code list WINS over the payroll area; only when the box is empty is the payroll area used.
    /// </summary>
    private async Task<List<int>> ResolveEmployeeIdsAsync(CostingRunInput input)
    {
        var requestedCodes = JournalVoucherBuilder.ParseEmployeeCodes(input.EmployeeCode);
        if (requestedCodes.Any())
        {
            return _employeeRepository.GetAll()
                .Where(e => requestedCodes.Contains(e.Code))
                .OrderBy(e => e.Code)
                .Select(e => e.Id)
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(input.PayrollAreaName))
        {
            return await _payrollAreaResolver
                .GetEmployeeIdsInPayrollAreaAsync(input.PayrollAreaName, input.PayInDate);
        }

        // Legacy lines 243-244 refuse to start with neither a payroll area nor an employee code.
        throw new UserFriendlyException(L("NoPayrollAreaOrEmployeeCodeEntered"));
    }

    /// <summary>
    /// Legacy <c>CurrencyService.GetCurrency(salReg.Employee)</c> (line 591). This port resolves it the
    /// same way the JV builder does -- the first detail row of the employee's basic pay covering the period
    /// -- rather than reaching for a "default currency", which this codebase has no concept of (see
    /// <see cref="CostRegisterDetail"/> class remarks). Returns <c>null</c> when unresolvable, which is
    /// valid since <c>CostRegisterDetail.CurrencyId</c> is nullable.
    /// </summary>
    private int? ResolveEmployeeCurrencyId(int employeeId, DateTime payForPeriod)
    {
        var basicPayId = _employeeBasicPayRepository.GetAll()
            .Where(p => p.EmployeeId == employeeId
                        && p.StartDate <= payForPeriod
                        && p.EndDate >= payForPeriod)
            .OrderBy(p => p.Id)
            .Select(p => (int?)p.Id)
            .FirstOrDefault();

        if (basicPayId == null)
        {
            return null;
        }

        return _employeeBasicPayDetailRepository.GetAll()
            .Where(d => d.EmployeeBasicPayId == basicPayId.Value)
            .OrderBy(d => d.Id)
            .Select(d => (int?)d.CurrencyId)
            .FirstOrDefault();
    }

    /// <summary>Legacy line 442.</summary>
    public static DateTime NormalizeToEndOfMonth(DateTime date)
    {
        return new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month), 0, 0, 0);
    }

    /// <summary>Legacy <c>ProjectLog</c> success message literal (line 620).</summary>
    public const string SuccessMessage = "Successfull";

    /// <summary>Legacy <c>ProjectLog.Message</c> truncation length (line 639).</summary>
    public const int MaxLogMessageLength = 1000;

    private class WageTypeRow
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string NameEn { get; set; }
        public bool IncludeInFIPosting { get; set; }
        public bool IncludeInArrearPay { get; set; }
        public int? ArrearSourceWageTypeId { get; set; }
    }

    private class WageAccountRow
    {
        public int WageTypeId { get; set; }
        public int AccountInfoId { get; set; }
        public int? CostElement1Id { get; set; }
        public int? CostElement2Id { get; set; }
        public int? CostElement3Id { get; set; }
        public int? CostElement4Id { get; set; }
        public int? CostElement5Id { get; set; }
        public int? CostElement6Id { get; set; }
        public int? CostElement7Id { get; set; }
        public double CostPercentage { get; set; }
    }
}
