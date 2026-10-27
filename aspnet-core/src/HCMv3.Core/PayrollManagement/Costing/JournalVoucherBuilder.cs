using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Costing;

/// <inheritdoc cref="IJournalVoucherBuilder"/>
[UnitOfWork]
public class JournalVoucherBuilder : HCMv3DomainServiceBase, IJournalVoucherBuilder
{
    /// <summary>
    /// Legacy <c>decimalPlaces</c> static (<c>uscCostingFile.ascx.cs</c> line 45). Per-line Debit/Credit
    /// and the Net Pay total are rounded to this many decimal places with
    /// <c>Math.Round(value, 3)</c> -- i.e. .NET's DEFAULT banker's rounding
    /// (<see cref="MidpointRounding.ToEven"/>), because legacy never passed a
    /// <see cref="MidpointRounding"/> argument. Preserved deliberately: switching to
    /// <see cref="MidpointRounding.AwayFromZero"/> would silently shift half-fils amounts relative to the
    /// live system.
    /// </summary>
    public const int DecimalPlaces = 3;

    /// <summary>
    /// Legacy <c>ProvisionWageTypeList</c> (<c>uscCostingFile.ascx.cs</c> lines 1776-1780), verbatim
    /// including the two commented-out arrear codes 7066/7067 which legacy deliberately left out.
    /// 1066 = Annual Leave Provision Increment Pay, 1067 = Indemnity Provision Increment Pay.
    ///
    /// <para>HARDCODED IN LEGACY. These are wage-type CODES baked into the screen source, not configuration.
    /// Ported as-is to preserve behaviour; a follow-up should promote them to
    /// <c>ManageFIPostingSettings</c>.</para>
    /// </summary>
    public static readonly string[] ProvisionWageTypeCodes = { "1066", "1067" };

    /// <summary>
    /// Legacy <c>empExcludeList</c> (<c>uscCostingFile.ascx.cs</c> lines 54-154). EVERY entry in the legacy
    /// list is commented out, so the effective list is EMPTY and the
    /// <c>!empExcludeList.Contains(...)</c> filter at line 1798 is a no-op. Kept as an explicit empty set so
    /// the filter stays visible rather than silently disappearing.
    /// </summary>
    public static readonly string[] ExcludedEmployeeCodes = { };

    /// <summary>
    /// Legacy <c>dateformat</c> static (<c>uscCostingFile.ascx.cs</c> line 46), used for the
    /// "Line number.Date" string column.
    ///
    /// <para>DIVERGENCE RESOLVED: <c>uscPostJV.ExportTable</c> formatted the same column as
    /// <c>"dd/MM/yyyy"</c> (<c>uscPostJV.ascx.cs</c> line 1213) while <c>uscCostingFile</c> uses
    /// <c>"MM/dd/yyyy"</c>. Since the JV that is actually POSTED to D365 does not read this column at all
    /// (the D365 payload uses <c>TransDate</c> formatted <c>yyyy-MM-dd</c>,
    /// <c>uscCostingFile.ascx.cs</c> line 1201), and the live/maintained screen is <c>uscCostingFile</c>,
    /// this port standardises on <c>uscCostingFile</c>'s <c>"MM/dd/yyyy"</c> for BOTH screens.</para>
    /// </summary>
    public const string LineDateFormat = "MM/dd/yyyy";

    private readonly IRepository<CostRegisterDetail> _costRegisterDetailRepository;
    private readonly IRepository<EmployeeWageAccount> _employeeWageAccountRepository;
    private readonly IRepository<EmployeeBasicPay> _employeeBasicPayRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _employeeBasicPayDetailRepository;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<AXBatch> _axBatchRepository;
    private readonly IRepository<Currency> _currencyRepository;
    private readonly IRepository<AccountInfo> _accountInfoRepository;
    private readonly IRepository<AccountType> _accountTypeRepository;
    private readonly IRepository<CostElement> _costElementRepository;
    private readonly IRepository<WageType> _wageTypeRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;

    public JournalVoucherBuilder(
        IRepository<CostRegisterDetail> costRegisterDetailRepository,
        IRepository<EmployeeWageAccount> employeeWageAccountRepository,
        IRepository<EmployeeBasicPay> employeeBasicPayRepository,
        IRepository<EmployeeBasicPayDetail> employeeBasicPayDetailRepository,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<AXBatch> axBatchRepository,
        IRepository<Currency> currencyRepository,
        IRepository<AccountInfo> accountInfoRepository,
        IRepository<AccountType> accountTypeRepository,
        IRepository<CostElement> costElementRepository,
        IRepository<WageType> wageTypeRepository,
        IRepository<Employee> employeeRepository,
        IEmployeePayrollAreaResolver payrollAreaResolver)
    {
        _costRegisterDetailRepository = costRegisterDetailRepository;
        _employeeWageAccountRepository = employeeWageAccountRepository;
        _employeeBasicPayRepository = employeeBasicPayRepository;
        _employeeBasicPayDetailRepository = employeeBasicPayDetailRepository;
        _payrollAreaRepository = payrollAreaRepository;
        _axBatchRepository = axBatchRepository;
        _currencyRepository = currencyRepository;
        _accountInfoRepository = accountInfoRepository;
        _accountTypeRepository = accountTypeRepository;
        _costElementRepository = costElementRepository;
        _wageTypeRepository = wageTypeRepository;
        _employeeRepository = employeeRepository;
        _payrollAreaResolver = payrollAreaResolver;
    }

    /// <summary>
    /// Legacy <c>GetEmpCodeList</c> (<c>uscCostingFile.ascx.cs</c> lines 2489-2498): split on comma, trim
    /// each token. Legacy does NOT drop empty tokens; this port drops them, because an empty token can only
    /// ever match an employee with a blank code and legacy's own callers guard the whole branch behind
    /// <c>empCode != ""</c>.
    /// </summary>
    public static List<string> ParseEmployeeCodes(string employeeCode)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            return new List<string>();
        }

        return employeeCode.Split(',')
            .Select(c => c.Trim())
            .Where(c => c.Length > 0)
            .ToList();
    }

    public async Task<JournalVoucherResult> BuildAsync(CostingRunInput input)
    {
        var result = new JournalVoucherResult();

        // ---- Validations: legacy uscCostingFile.ascx.cs lines 1765-1781 ----------------------------
        var axBatch = _axBatchRepository.GetAll().OrderBy(b => b.Id).FirstOrDefault();
        if (axBatch == null)
        {
            throw new UserFriendlyException(L("AXBatchNoRecordFound"));
        }

        var payrollArea = _payrollAreaRepository.GetAll()
            .FirstOrDefault(a => a.Name == input.PayrollAreaName);
        if (payrollArea == null)
        {
            throw new UserFriendlyException(L("PayrollAreaNotFound"));
        }

        if (payrollArea.FICompanyId == null)
        {
            throw new UserFriendlyException(L("PayrollFICompanyNull"));
        }

        if (payrollArea.FIBankId == null)
        {
            throw new UserFriendlyException(L("PayrollFIBankNull"));
        }

        // ---- Source rows: legacy lines 1784-1799 ---------------------------------------------------
        // Legacy predicate `r.CostRegister.Employee.OrgAssignment.Any(rd => rd.StartDate <= payInDate &&
        // rd.EndDate >= payInDate && rd.PayrollArea == Payrollarea)` cannot be expressed directly here:
        // this port's EmployeeOrgAssignment has NO PayrollArea FK. IEmployeePayrollAreaResolver is the
        // established substitute (org assignment -> org unit -> OrgUnitProfile.PayrollAreaId); see that
        // interface's remarks for the behavioural deltas that carries.
        var payrollAreaEmployeeIds = await _payrollAreaResolver
            .GetEmployeeIdsInPayrollAreaAsync(input.PayrollAreaName, input.PayInDate);

        if (!payrollAreaEmployeeIds.Any())
        {
            return result;
        }

        var employeeIdSet = new HashSet<int>(payrollAreaEmployeeIds);

        var details = _costRegisterDetailRepository.GetAll()
            .Where(r => r.CostRegister.Period == input.PayInDate
                        && r.CostRegister.PayrollRunType == input.PayrollRunType
                        && r.WageType.Code != NetPayWageTypeCode)
            .Select(r => new CostDetailRow
            {
                EmployeeId = r.CostRegister.EmployeeId,
                WageTypeId = r.WageTypeId,
                AccountInfoId = r.AccountInfoId,
                CostElement1Id = r.CostElement1Id,
                CostElement2Id = r.CostElement2Id,
                CostElement3Id = r.CostElement3Id,
                CostElement4Id = r.CostElement4Id,
                CostElement5Id = r.CostElement5Id,
                CostElement6Id = r.CostElement6Id,
                CostElement7Id = r.CostElement7Id,
                Amount = r.Amount
            })
            .ToList()
            .Where(r => employeeIdSet.Contains(r.EmployeeId))
            .ToList();

        if (!details.Any())
        {
            return result;
        }

        // ---- Lookup dictionaries (Core has no EF Core reference: plain in-memory joins) ------------
        var employees = _employeeRepository.GetAll()
            .Where(e => employeeIdSet.Contains(e.Id))
            .Select(e => new { e.Id, e.Code })
            .ToList()
            .ToDictionary(e => e.Id, e => e.Code);

        var wageTypes = _wageTypeRepository.GetAll()
            .Select(w => new WageTypeRow
            {
                Id = w.Id,
                Code = w.Code,
                NameEn = w.NameEn,
                BillingType = w.BillingType,
                PayrollOrder = w.PayrollOrder
            })
            .ToList()
            .ToDictionary(w => w.Id, w => w);

        var accountInfos = _accountInfoRepository.GetAll()
            .Select(a => new AccountInfoRow
            {
                Id = a.Id,
                Code = a.Code,
                NameEn = a.NameEn,
                AccountTypeId = a.AccountTypeId
            })
            .ToList()
            .ToDictionary(a => a.Id, a => a);

        var accountTypeNames = _accountTypeRepository.GetAll()
            .Select(t => new { t.Id, t.NameEn })
            .ToList()
            .ToDictionary(t => t.Id, t => t.NameEn);

        var costElementCodes = _costElementRepository.GetAll()
            .Select(c => new { c.Id, c.Code })
            .ToList()
            .ToDictionary(c => c.Id, c => c.Code);

        // Legacy line 1795: filter by the comma-separated employee-code box, then order by employee code.
        var requestedCodes = ParseEmployeeCodes(input.EmployeeCode);
        if (requestedCodes.Any())
        {
            var codeSet = new HashSet<string>(requestedCodes, StringComparer.Ordinal);
            details = details
                .Where(r => employees.TryGetValue(r.EmployeeId, out var code) && codeSet.Contains(code))
                .ToList();
        }

        // Legacy line 1798-1799: drop excluded codes, then order by employee code, then wage-type
        // PayrollOrder. That ordering is load-bearing: RecId/Line number are assigned by iteration order.
        var excludeSet = new HashSet<string>(ExcludedEmployeeCodes, StringComparer.Ordinal);
        details = details
            .Where(r => !(employees.TryGetValue(r.EmployeeId, out var code) && excludeSet.Contains(code)))
            .OrderBy(r => employees.TryGetValue(r.EmployeeId, out var code) ? code : null, StringComparer.Ordinal)
            .ThenBy(r => wageTypes.TryGetValue(r.WageTypeId, out var wt) ? wt.PayrollOrder : 0)
            .ToList();

        // ---- Employee wage accounts: legacy lines 1862-1880 ----------------------------------------
        var wageAccounts = _employeeWageAccountRepository.GetAll()
            .Where(r => r.StartDate <= input.PayInDate && r.EndDate >= input.PayInDate)
            .Select(r => new WageAccountRow
            {
                EmployeeId = r.EmployeeId,
                WageTypeId = r.WageTypeId,
                CostElement1Id = r.CostElement1Id,
                CostElement2Id = r.CostElement2Id,
                CostElement3Id = r.CostElement3Id,
                CostElement4Id = r.CostElement4Id,
                CostElement5Id = r.CostElement5Id,
                CostElement6Id = r.CostElement6Id,
                CostElement7Id = r.CostElement7Id,
                OffsetAccountInfoId = r.OffsetAccountInfoId,
                MergeOffsetAccount = r.MergeOffsetAccount
            })
            .ToList();

        // ---- Employee basic-pay currency short form: legacy lines 1993-2001 -------------------------
        var basicPayCurrency = BuildBasicPayCurrencyMap(employeeIdSet, input.PayInDate);

        // ---- Projection: legacy lines 1881-1940 ----------------------------------------------------
        var projected = details.Select(rec => Project(rec, axBatch, input, employees, wageTypes,
                                                     accountInfos, accountTypeNames, costElementCodes,
                                                     wageAccounts))
                               .ToList();

        // ---- Row emission: legacy lines 1942-2115 --------------------------------------------------
        var count = 1;
        foreach (var rec in projected)
        {
            // Legacy line 1945: lines with neither a debit nor a credit are skipped entirely.
            if (rec.Debit == 0 && rec.Credit == 0)
            {
                continue;
            }

            var currency = basicPayCurrency.TryGetValue(rec.EmployeeId, out var c) ? c : "";
            var isCustomer = rec.LineNumberAccountType == CustomerAccountTypeName;

            // ---- Main account line (legacy lines 1947-2020) ----
            var line = new JournalVoucherLine
            {
                Valid = "",
                CompanyAccounts = rec.CompanyAccounts,
                JournalBatchNumber = axBatch.JournalBatchNumber,
                RecId = count,
                LineNumber = count,
                LineNumberDate = input.PostingDate.ToString(LineDateFormat),
                LineNumberAccountType = rec.LineNumberAccountType,
                LedgerDimensionMainAccount = rec.LedgerDimensionMainAccount,
                LedgerDimensionMainAccountName = rec.LedgerDimensionMainAccountName,
                DefaultDimensionLocation = rec.DefaultDimensionLocation,

                // Legacy lines 1961-1965: for a Customer line the employee dimension moves from the
                // "LedgerDimension" family to "DefaultDimension", and Location/Department/Employees are
                // blanked.
                DefaultDimensionEmployees = isCustomer ? rec.LedgerDimensionEmployees : rec.DefaultDimensionEmployees,
                LedgerDimensionLocation = isCustomer ? "" : rec.LedgerDimensionLocation,
                LedgerDimensionBrand = rec.LedgerDimensionBrand,
                LedgerDimensionDepartment = isCustomer ? "" : rec.LedgerDimensionDepartment,
                LedgerDimensionEmployees = isCustomer ? "" : rec.LedgerDimensionEmployees,

                Currency = currency,
                ExchangeRate = "",
                Debit = rec.Debit,
                Credit = rec.Credit,
                Description = rec.Description,
                Voucher = "",

                // Legacy lines 2009-2011: these three cells are deliberately left blank on the MAIN line
                // (the assignments are commented out); the offset values are only used to build the
                // offset line below.
                OffsetAccountType = "",
                OffsetLedgerDimensionMainAccount = "",
                OffsetCompanyAccounts = "",

                BillingType = rec.BillingTypeLabel,
                IsMerged = false,
                TransDate = input.PostingDate,
                EmpCode = rec.EmployeeCode
            };

            result.Lines.Add(line);
            count++;

            // ---- Offset line (legacy lines 2022-2113) ----
            // Emitted only for provision lines that actually have an offset account configured.
            var isProvision = ProvisionWageTypeCodes.Contains(rec.WageTypeCode)
                              || rec.BillingType == HCMv3.PayrollManagement.BillingType.Provision;

            if (!isProvision || string.IsNullOrEmpty(rec.OffsetLedgerDimensionMainAccount))
            {
                continue;
            }

            // Legacy lines 2027-2029: look for ANY projected row of the same employee+wage type whose
            // wage account has MergeOffsetAccount set. Note the legacy quirk faithfully reproduced: the
            // merged row's DIMENSIONS come from recMerge while the AMOUNTS come from rec.
            var recMerge = projected.FirstOrDefault(r => r.EmployeeId == rec.EmployeeId
                                                         && r.WageTypeId == rec.WageTypeId
                                                         && r.MergeOffsetAccount);

            if (recMerge == null)
            {
                // Plain offset line: Debit and Credit are SWAPPED against the main line
                // (legacy lines 2051-2052). This is what makes each provision pair self-balancing.
                result.Lines.Add(new JournalVoucherLine
                {
                    Valid = "",
                    CompanyAccounts = rec.CompanyAccounts,
                    JournalBatchNumber = axBatch.JournalBatchNumber,
                    RecId = count,
                    LineNumber = count,
                    LineNumberDate = input.PostingDate.ToString(LineDateFormat),
                    LineNumberAccountType = rec.OffsetAccountType,
                    LedgerDimensionMainAccount = rec.OffsetLedgerDimensionMainAccount,

                    // Legacy does NOT set LedgerDimension.MainAccountName on the offset line.
                    LedgerDimensionMainAccountName = null,

                    // Legacy lines 2043-2048 use the UNADJUSTED dimensions here -- the Customer
                    // blanking applied to the main line above is NOT repeated.
                    DefaultDimensionLocation = rec.DefaultDimensionLocation,
                    DefaultDimensionEmployees = rec.DefaultDimensionEmployees,
                    LedgerDimensionLocation = rec.LedgerDimensionLocation,
                    LedgerDimensionBrand = rec.LedgerDimensionBrand,
                    LedgerDimensionDepartment = rec.LedgerDimensionDepartment,
                    LedgerDimensionEmployees = rec.LedgerDimensionEmployees,

                    Currency = currency,
                    ExchangeRate = "",
                    Debit = rec.Credit,
                    Credit = rec.Debit,
                    Description = rec.Description,
                    Voucher = "",
                    BillingType = rec.BillingTypeLabel,
                    IsMerged = false,
                    TransDate = input.PostingDate,
                    EmpCode = rec.EmployeeCode
                });

                count++;
            }
            else
            {
                // Merge mode (legacy lines 2067-2111): accumulate into the single existing merged row
                // keyed on (LedgerDimension.Employees, LedgerDimension.MainAccount, IsMerged == true).
                var merged = result.Lines.FirstOrDefault(l => l.IsMerged
                                                              && l.LedgerDimensionEmployees == recMerge.LedgerDimensionEmployees
                                                              && l.LedgerDimensionMainAccount == recMerge.OffsetLedgerDimensionMainAccount);

                if (merged == null)
                {
                    merged = new JournalVoucherLine { RecId = count, LineNumber = count };
                    result.Lines.Add(merged);
                    count++;
                }

                merged.Valid = "";
                merged.CompanyAccounts = recMerge.CompanyAccounts;
                merged.JournalBatchNumber = axBatch.JournalBatchNumber;
                merged.LineNumberDate = input.PostingDate.ToString(LineDateFormat);
                merged.LineNumberAccountType = recMerge.OffsetAccountType;
                merged.LedgerDimensionMainAccount = recMerge.OffsetLedgerDimensionMainAccount;
                merged.DefaultDimensionLocation = recMerge.DefaultDimensionLocation;
                merged.DefaultDimensionEmployees = recMerge.DefaultDimensionEmployees;
                merged.LedgerDimensionLocation = recMerge.LedgerDimensionLocation;
                merged.LedgerDimensionBrand = recMerge.LedgerDimensionBrand;
                merged.LedgerDimensionDepartment = recMerge.LedgerDimensionDepartment;
                merged.LedgerDimensionEmployees = recMerge.LedgerDimensionEmployees;
                merged.Currency = currency;
                merged.ExchangeRate = "";

                // Legacy lines 2100-2101: accumulate the SWAPPED amounts of the CURRENT rec.
                merged.Debit += rec.Credit;
                merged.Credit += rec.Debit;

                merged.Description = recMerge.Description;
                merged.Voucher = "";
                merged.BillingType = recMerge.BillingTypeLabel;
                merged.IsMerged = true;
                merged.TransDate = input.PostingDate;
                merged.EmpCode = recMerge.EmployeeCode;
            }
        }

        // ---- Net Pay balancing line: legacy lines 2118-2165 -----------------------------------------
        if (result.Lines.Any())
        {
            AppendNetPayLine(result, projected, payrollArea, axBatch, input, accountInfos,
                             accountTypeNames, count);
        }

        return result;
    }

    /// <summary>
    /// Legacy net-pay block (<c>uscCostingFile.ascx.cs</c> lines 2118-2165).
    ///
    /// <para><b>Why provisions are excluded from the total (line 2121).</b> Each provision line already has a
    /// matching offset line with swapped Debit/Credit, so a provision contributes ZERO to the JV's net
    /// balance. Including it in the net-pay total would post its amount a second time against the bank
    /// account and break debit=credit. <c>uscPostJV</c>'s older copy (line 1294) omitted this exclusion --
    /// that is a genuine legacy bug, not a feature, and it is not carried forward.</para>
    ///
    /// <para><b>Sign.</b> The total is <c>SUM(Debit - Credit)</c> over Earning and Deduction lines, and is
    /// written to the CREDIT column unconditionally (legacy lines 2151-2152). A negative net therefore lands
    /// as a NEGATIVE CREDIT, not as a positive debit -- legacy never flips it, and neither does this port.</para>
    /// </summary>
    private void AppendNetPayLine(
        JournalVoucherResult result,
        List<ProjectedRow> projected,
        PayrollArea payrollArea,
        AXBatch axBatch,
        CostingRunInput input,
        Dictionary<int, AccountInfoRow> accountInfos,
        Dictionary<int, string> accountTypeNames,
        int count)
    {
        var totalNetAmount = projected
            .Where(r => !ProvisionWageTypeCodes.Contains(r.WageTypeCode)
                        && (r.BillingType == HCMv3.PayrollManagement.BillingType.Earning
                            || r.BillingType == HCMv3.PayrollManagement.BillingType.Deduction))
            .Sum(r => r.Debit - r.Credit);

        // Legacy line 2125: a perfectly-zero net emits NO net-pay line at all.
        if (totalNetAmount == 0)
        {
            return;
        }

        string fiBankCode = null;
        string fiBankAccountTypeName = null;
        if (payrollArea.FIBankId != null && accountInfos.TryGetValue(payrollArea.FIBankId.Value, out var fiBank))
        {
            fiBankCode = fiBank.Code;
            accountTypeNames.TryGetValue(fiBank.AccountTypeId, out fiBankAccountTypeName);
        }

        var fiCompanyCode = payrollArea.FICompanyId == null
            ? null
            : _costElementRepository.GetAll()
                .Where(c => c.Id == payrollArea.FICompanyId.Value)
                .Select(c => c.Code)
                .FirstOrDefault();

        var payrollAreaCurrency = _currencyRepository.GetAll()
            .Where(c => c.Id == payrollArea.CurrencyId)
            .Select(c => c.CurrencyShortFormEn)
            .FirstOrDefault() ?? "";

        result.Lines.Add(new JournalVoucherLine
        {
            Valid = "",
            CompanyAccounts = fiCompanyCode,
            JournalBatchNumber = axBatch.JournalBatchNumber,
            RecId = count,
            LineNumber = count,
            LineNumberDate = input.PostingDate.ToString(LineDateFormat),
            LineNumberAccountType = fiBankAccountTypeName,
            LedgerDimensionMainAccount = fiBankCode,

            // Legacy leaves every dimension cell unset on this row (lines 2138-2146 are commented out).
            LedgerDimensionMainAccountName = null,
            DefaultDimensionLocation = null,
            DefaultDimensionEmployees = null,
            LedgerDimensionLocation = null,
            LedgerDimensionBrand = null,
            LedgerDimensionDepartment = null,
            LedgerDimensionEmployees = null,

            Currency = payrollAreaCurrency,
            ExchangeRate = "",
            Debit = 0,
            Credit = Math.Round(totalNetAmount, DecimalPlaces),
            Description = NetPayDescription,
            Voucher = "",
            OffsetAccountType = "",
            OffsetLedgerDimensionMainAccount = "",
            OffsetCompanyAccounts = "",
            BillingType = HCMv3.PayrollManagement.BillingType.Earning.ToString(),
            IsMerged = false,
            TransDate = input.PostingDate,
            EmpCode = ""
        });
    }

    /// <summary>
    /// The per-row projection of legacy lines 1881-1938.
    /// </summary>
    private ProjectedRow Project(
        CostDetailRow rec,
        AXBatch axBatch,
        CostingRunInput input,
        Dictionary<int, string> employees,
        Dictionary<int, WageTypeRow> wageTypes,
        Dictionary<int, AccountInfoRow> accountInfos,
        Dictionary<int, string> accountTypeNames,
        Dictionary<int, string> costElementCodes,
        List<WageAccountRow> wageAccounts)
    {
        string CostElementCode(int? id) =>
            id != null && costElementCodes.TryGetValue(id.Value, out var code) ? code : null;

        wageTypes.TryGetValue(rec.WageTypeId, out var wageType);
        accountInfos.TryGetValue(rec.AccountInfoId, out var accountInfo);

        string accountTypeName = null;
        if (accountInfo != null)
        {
            accountTypeNames.TryGetValue(accountInfo.AccountTypeId, out accountTypeName);
        }

        var billingType = wageType?.BillingType ?? HCMv3.PayrollManagement.BillingType.Earning;
        var wageTypeCode = wageType?.Code;

        // Legacy lines 1909-1912: the offset account/type come from the FIRST wage account matching only
        // employee + wage type -- the cost elements are deliberately NOT part of this match.
        var offsetSource = wageAccounts.FirstOrDefault(r => r.EmployeeId == rec.EmployeeId
                                                            && r.WageTypeId == rec.WageTypeId);

        string offsetAccountCode = "";
        string offsetAccountTypeName = "";
        if (offsetSource?.OffsetAccountInfoId != null
            && accountInfos.TryGetValue(offsetSource.OffsetAccountInfoId.Value, out var offsetAccount))
        {
            offsetAccountCode = offsetAccount.Code;
            accountTypeNames.TryGetValue(offsetAccount.AccountTypeId, out offsetAccountTypeName);
            offsetAccountTypeName ??= "";
        }

        // Legacy lines 1917-1937: MergeOffsetAccount is resolved by an EXACT match on employee + wage type
        // + ALL SEVEN cost-element slots. A single differing slot means "do not merge".
        var mergeSource = wageAccounts.FirstOrDefault(r => r.EmployeeId == rec.EmployeeId
                                                           && r.WageTypeId == rec.WageTypeId
                                                           && r.CostElement1Id == rec.CostElement1Id
                                                           && r.CostElement2Id == rec.CostElement2Id
                                                           && r.CostElement3Id == rec.CostElement3Id
                                                           && r.CostElement4Id == rec.CostElement4Id
                                                           && r.CostElement5Id == rec.CostElement5Id
                                                           && r.CostElement6Id == rec.CostElement6Id
                                                           && r.CostElement7Id == rec.CostElement7Id);

        return new ProjectedRow
        {
            EmployeeId = rec.EmployeeId,
            EmployeeCode = employees.TryGetValue(rec.EmployeeId, out var empCode) ? empCode : "",
            WageTypeId = rec.WageTypeId,
            WageTypeCode = wageTypeCode,
            BillingType = billingType,

            // Cost-element slot 1 is the FI company (legacy line 1884).
            CompanyAccounts = CostElementCode(rec.CostElement1Id),

            LineNumberAccountType = accountTypeName,
            LedgerDimensionMainAccount = accountInfo?.Code,
            LedgerDimensionMainAccountName = accountInfo?.NameEn,

            // Cost-element slot -> GL dimension mapping, legacy lines 1896-1901. Note it is NOT 1:1 by
            // index: slot 7 -> DefaultDimension.Location, slot 6 -> DefaultDimension.Employees,
            // slot 2 -> Location, slot 3 -> Brand, slot 4 -> Department, slot 5 -> Employees.
            DefaultDimensionLocation = CostElementCode(rec.CostElement7Id),
            DefaultDimensionEmployees = CostElementCode(rec.CostElement6Id),
            LedgerDimensionLocation = CostElementCode(rec.CostElement2Id),
            LedgerDimensionBrand = CostElementCode(rec.CostElement3Id),
            LedgerDimensionDepartment = CostElementCode(rec.CostElement4Id),
            LedgerDimensionEmployees = CostElementCode(rec.CostElement5Id),

            // Legacy lines 1904-1905. Earning and Provision -> Debit; Deduction -> Credit; TimeType -> 0/0
            // (and therefore skipped by the `Debit != 0 || Credit != 0` gate).
            Debit = Math.Round(
                billingType == HCMv3.PayrollManagement.BillingType.Earning
                || billingType == HCMv3.PayrollManagement.BillingType.Provision
                    ? rec.Amount
                    : 0,
                DecimalPlaces),
            Credit = Math.Round(
                billingType == HCMv3.PayrollManagement.BillingType.Deduction ? rec.Amount : 0,
                DecimalPlaces),

            Description = wageType?.NameEn,
            OffsetAccountType = offsetAccountTypeName,
            OffsetLedgerDimensionMainAccount = offsetAccountCode,

            // Legacy line 1916: the hardcoded provision codes override the configured billing type label.
            BillingTypeLabel = ProvisionWageTypeCodes.Contains(wageTypeCode)
                ? ProvisionBillingTypeLabel
                : billingType.ToString(),

            MergeOffsetAccount = mergeSource?.MergeOffsetAccount ?? false
        };
    }

    /// <summary>
    /// Legacy lines 1993-2001: the currency printed on every line of an employee's JV is the
    /// <c>CurrencyShortFormEn</c> of the FIRST detail row of the employee's basic pay covering the period --
    /// NOT <c>CostRegisterDetail.Currency</c>, despite that being projected at line 1902. Employees with no
    /// covering basic-pay row get a blank currency cell, exactly as legacy left it.
    /// </summary>
    private Dictionary<int, string> BuildBasicPayCurrencyMap(HashSet<int> employeeIds, DateTime payInDate)
    {
        var basicPays = _employeeBasicPayRepository.GetAll()
            .Where(p => p.StartDate <= payInDate && p.EndDate >= payInDate)
            .Select(p => new { p.Id, p.EmployeeId })
            .ToList()
            .Where(p => employeeIds.Contains(p.EmployeeId))
            .ToList();

        if (!basicPays.Any())
        {
            return new Dictionary<int, string>();
        }

        var basicPayIds = basicPays.Select(p => p.Id).ToList();

        var firstDetailCurrencyByBasicPay = _employeeBasicPayDetailRepository.GetAll()
            .Where(d => basicPayIds.Contains(d.EmployeeBasicPayId))
            .Select(d => new { d.Id, d.EmployeeBasicPayId, d.CurrencyId })
            .ToList()
            .GroupBy(d => d.EmployeeBasicPayId)
            .ToDictionary(g => g.Key, g => g.OrderBy(d => d.Id).First().CurrencyId);

        var currencyShortForms = _currencyRepository.GetAll()
            .Select(c => new { c.Id, c.CurrencyShortFormEn })
            .ToList()
            .ToDictionary(c => c.Id, c => c.CurrencyShortFormEn);

        var map = new Dictionary<int, string>();
        foreach (var basicPay in basicPays.OrderBy(p => p.Id))
        {
            if (map.ContainsKey(basicPay.EmployeeId))
            {
                continue;
            }

            if (firstDetailCurrencyByBasicPay.TryGetValue(basicPay.Id, out var currencyId)
                && currencyShortForms.TryGetValue(currencyId, out var shortForm))
            {
                map[basicPay.EmployeeId] = shortForm ?? "";
            }
        }

        return map;
    }

    /// <summary>Legacy wage-type code for Net Pay, excluded from the cost register query (legacy line 1790).</summary>
    public const string NetPayWageTypeCode = "9000";

    /// <summary>Legacy literal written into the Description cell of the balancing line (line 2153).</summary>
    public const string NetPayDescription = "Net Pay";

    /// <summary>Legacy literal used to route lines into the separate provision JV (line 1916).</summary>
    public const string ProvisionBillingTypeLabel = "Provision";

    /// <summary>
    /// The <c>AccountType.NameEn</c> value legacy string-compares against to apply the Customer dimension
    /// rearrangement (lines 1961-1965) and the D365 <c>Cust</c> account-type substitution (line 1238).
    /// Legacy compares raw English names; there is no enum behind it.
    /// </summary>
    public const string CustomerAccountTypeName = "Customer";

    private class WageTypeRow
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string NameEn { get; set; }
        public BillingType BillingType { get; set; }
        public int PayrollOrder { get; set; }
    }

    private class AccountInfoRow
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string NameEn { get; set; }
        public int AccountTypeId { get; set; }
    }

    private class CostDetailRow
    {
        public int EmployeeId { get; set; }
        public int WageTypeId { get; set; }
        public int AccountInfoId { get; set; }
        public int? CostElement1Id { get; set; }
        public int? CostElement2Id { get; set; }
        public int? CostElement3Id { get; set; }
        public int? CostElement4Id { get; set; }
        public int? CostElement5Id { get; set; }
        public int? CostElement6Id { get; set; }
        public int? CostElement7Id { get; set; }
        public double Amount { get; set; }
    }

    private class WageAccountRow
    {
        public int EmployeeId { get; set; }
        public int WageTypeId { get; set; }
        public int? CostElement1Id { get; set; }
        public int? CostElement2Id { get; set; }
        public int? CostElement3Id { get; set; }
        public int? CostElement4Id { get; set; }
        public int? CostElement5Id { get; set; }
        public int? CostElement6Id { get; set; }
        public int? CostElement7Id { get; set; }
        public int? OffsetAccountInfoId { get; set; }
        public bool MergeOffsetAccount { get; set; }
    }

    private class ProjectedRow
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public int WageTypeId { get; set; }
        public string WageTypeCode { get; set; }
        public BillingType BillingType { get; set; }
        public string CompanyAccounts { get; set; }
        public string LineNumberAccountType { get; set; }
        public string LedgerDimensionMainAccount { get; set; }
        public string LedgerDimensionMainAccountName { get; set; }
        public string DefaultDimensionLocation { get; set; }
        public string DefaultDimensionEmployees { get; set; }
        public string LedgerDimensionLocation { get; set; }
        public string LedgerDimensionBrand { get; set; }
        public string LedgerDimensionDepartment { get; set; }
        public string LedgerDimensionEmployees { get; set; }
        public double Debit { get; set; }
        public double Credit { get; set; }
        public string Description { get; set; }
        public string OffsetAccountType { get; set; }
        public string OffsetLedgerDimensionMainAccount { get; set; }
        public string BillingTypeLabel { get; set; }
        public bool MergeOffsetAccount { get; set; }
    }
}
