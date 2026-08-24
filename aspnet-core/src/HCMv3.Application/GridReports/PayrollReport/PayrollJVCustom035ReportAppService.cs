using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.PayrollReport;

/// <summary>
/// Payroll JV (Custom 035) analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/PayrollReport/uscRptPayrollJV_Custom035.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 558-1224).
///
/// THREE SHEETS BEHIND ONE SCREEN (<c>cmbType</c>): Main Format (13 columns, one line per
/// account/cost-element combination), Missing Accounts (14) and EmployeeWise BreakDown (15, the same
/// voucher lines broken out per employee and wage type). They share only the preamble -- the filtered
/// <c>SalaryRegisterDetail</c> left-joined to <c>EmployeeWageAccount</c> (lines 568-693).
///
/// COST-ELEMENT SLOTS DIFFER FROM ITS SIBLINGS: here CostElement1 is Department, 2 is StaffCodes, 3 is
/// DepartmentAllocation and 7 is Sector (lines 745-751). Custom 045 uses 1/2/3 as Branch/Sector/Department
/// and Custom 030 uses 1/2 as Department/Company. The slots are per-customer configuration, so nothing may
/// be shared between these ports at that level.
///
/// THE PAYROLL AREA SUPPLIES TWO VOUCHER FIELDS, not just a filter: <c>PayrollArea.FICompany.Code</c> fills
/// "Company accounts" on every row and <c>PayrollArea.FIBank</c> fills the net-salary row's account
/// (lines 692, 840-856). Legacy resolved the payroll area ONLY when the combo was set, so running the
/// report with no payroll area yields a voucher with a blank company and a blank bank account. Reproduced.
///
/// ENGLISH NAMES ONLY, DELIBERATELY: legacy reads <c>NameEn</c> throughout with no
/// <c>CurrentAspect == "ar-KW"</c> switch, so <see cref="GridReportAppServiceBase.UseArabicNames"/> is not
/// consulted -- these are finance master-data keys, not UI labels.
///
/// === FLAGGED ACCOUNTING DEFECTS, REPRODUCED (each pinned by a test named to say so) ===
///
/// 1. NET PAY IS COUNTED TWICE, OR NEVER. The Credit Entry loop takes EVERY Deduction wage type with no
///    exclusion for the net-pay code (line 800) -- unlike the sibling Custom 045, which explicitly pairs
///    "Deduction OR 9000". A separate Net Salary Entry then credits the bank with the sum of wage type 9000
///    (line 843). So if 9000 is a Deduction with <c>IncludeInFIPosting</c> on it is credited twice and the
///    voucher is out by the net payroll; if it is not in the posting set the bank row is always zero and the
///    voucher never balances. Either way the sheet does not foot.
///
/// 2. THE NET SALARY ROW IS ALWAYS EMITTED. It is appended outside any conditional (lines 838-869), so an
///    empty period still produces one voucher line -- with a blank account when no payroll area was chosen.
///
/// 3. ON EMPLOYEEWISE BREAKDOWN THE NET SALARY ROW HAS NO EMPLOYEE. That sheet is per-employee, yet the
///    trailing bank row leaves EmpCode and WageType unset (lines 1196-1220), so the whole payroll's net pay
///    appears against a blank employee.
///
/// 4. THE ACCOUNT STRING SWALLOWS MISSING DIMENSIONS. It is built by concatenation with a fixed "---"
///    filler (lines 771-777), so a null cost element contributes an empty segment and an account with a
///    missing Department is textually indistinguishable from one whose Department code is blank.
///
/// DELIBERATE DIVERGENCE: ROW LEVEL SECURITY IS APPLIED. Legacy declares <c>ApplyRowLevelSecurity</c> and
/// never calls it. Applied here (empty list == no restriction), an agreed wave-wide divergence.
///
/// NOT PORTED -- DevExpress/WebForms plumbing with no data meaning: the drill-down session stack, the
/// ColumnChooser JSON persistence, the callback-panel state machine, the PDF export button, the dead
/// <c>Table1_Old()</c> (lines 1225-1900, an earlier formulation of the same three sheets) and the fully
/// commented-out "Provision Format" branch (lines 873-960).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_PayrollJVCustom035)]
public class PayrollJVCustom035ReportAppService : GridReportAppServiceBase, IPayrollJVCustom035ReportAppService
{
    private const string NetPayWageTypeCode = "9000";
    private const string PaymentStatusNone = "None";

    // --- Main Format / EmployeeWise BreakDown captions (legacy lines 712-724 and 1035-1049) ---
    private const string ColEmpCode = "EmpCode";
    private const string ColWageType = "WageType";
    private const string ColVoucher = "Voucher";
    private const string ColDate = "Date";
    private const string ColCompanyAccounts = "Company accounts";
    private const string ColAccountType = "Account type";
    private const string ColAccount = "Account";
    private const string ColDescription = "Description";
    private const string ColDebit = "Debit";
    private const string ColCredit = "Credit";
    private const string ColOffsetAccountType = "Offset account type";
    private const string ColOffsetAccount = "Offset account";
    private const string ColPaymentStatus = "Payment status";
    private const string ColAccountDetails = "Account details";

    // --- Missing Accounts captions (legacy lines 955-990) ---
    private const string MaForPeriod = "ForPeriod";
    private const string MaAccountInfoCode = "AccountInfo Code";
    private const string MaAccountInfoName = "AccountInfo Name";
    private const string MaOffsetAccountInfoCode = "OffsetAccountInfo Code";
    private const string MaOffsetAccountInfoName = "OffsetAccountInfo Name";
    private const string MaDepartments = "Departments";
    private const string MaStaffCodes = "StaffCodes";
    private const string MaDepartmentAllocation = "DepartmentAllocation";
    private const string MaSector = "Sector";
    private const string MaCostPercentage = "CostPercentage";
    private const string MaAmount = "Amount";

    private static readonly (string Name, bool IsNumeric)[] MainFormatColumns =
    {
        (ColVoucher, false),
        (ColDate, false),
        (ColCompanyAccounts, false),
        (ColAccountType, false),
        (ColAccount, false),
        (ColDescription, false),
        (ColDebit, true),
        (ColCredit, true),
        (ColOffsetAccountType, false),
        (ColOffsetAccount, false),
        (ColPaymentStatus, false),
        (ColAccountDetails, false)
    };

    private static readonly (string Name, bool IsNumeric)[] MissingAccountsColumns =
    {
        (ColEmpCode, false),
        (MaForPeriod, false),
        (ColWageType, false),
        (MaAccountInfoCode, false),
        (MaAccountInfoName, false),
        (MaOffsetAccountInfoCode, false),
        (MaOffsetAccountInfoName, false),
        (MaDepartments, false),
        (MaStaffCodes, false),
        (MaDepartmentAllocation, false),
        (MaSector, false),
        (MaCostPercentage, true),
        (MaAmount, true)
    };

    private static readonly (string Name, bool IsNumeric)[] BreakDownColumns =
    {
        (ColEmpCode, false),
        (ColWageType, false),
        (ColVoucher, false),
        (ColDate, false),
        (ColCompanyAccounts, false),
        (ColAccountType, false),
        (ColAccount, false),
        (ColDescription, false),
        (ColDebit, true),
        (ColCredit, true),
        (ColOffsetAccountType, false),
        (ColOffsetAccount, false),
        (ColPaymentStatus, false),
        (ColAccountDetails, false)
    };

    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<EmployeeWageAccount> _employeeWageAccountRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;

    public PayrollJVCustom035ReportAppService(
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<EmployeeWageAccount> employeeWageAccountRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<PayrollArea> payrollAreaRepository)
    {
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _employeeWageAccountRepository = employeeWageAccountRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _payrollAreaRepository = payrollAreaRepository;
    }

    protected override string LegacyScreenName => "uscRptPayrollJV_Custom035";

    protected override string ScreenCaption => "Payroll JV";

    [HttpPost]
    public async Task<PayrollJVCustom035ReportOutput> GetPayrollJVCustom035Report(
        GetPayrollJVCustom035ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = query.Count();

        var rows = query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildJvParameterString(input), totalCount);

        return new PayrollJVCustom035ReportOutput
        {
            TotalCount = totalCount,
            Items = rows,
            Columns = GetColumnDefinitions(input.ReportType)
                .Select(c => new PayrollJVCustom035ReportColumnDto { Name = c.Name, IsNumeric = c.IsNumeric })
                .ToList()
        };
    }

    [HttpPost]
    public async Task<FileDto> GetPayrollJVCustom035ReportToExcel(GetPayrollJVCustom035ReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set,
        // and a partial journal voucher is worse than no journal voucher.
        var rows = query
            .OrderBy(input.Sorting)
            .ToList();

        return await ExportToFileAsync(
            "PayrollJVCustom035.xlsx",
            rows,
            GetExportColumns(input.ReportType),
            BuildJvParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid. Ports legacy
    /// <c>Table1()</c>: the shared preamble (lines 568-693) then the selected type's fill loop.
    /// </summary>
    private async Task<IQueryable<PayrollJVCustom035ReportRowDto>> BuildQueryAsync(
        GetPayrollJVCustom035ReportInput input)
    {
        var periodEnd = ResolvePeriodEnd(input);
        var lines = await LoadJoinedLinesAsync(input, periodEnd);
        var payrollArea = await LoadPayrollAreaAsync(input.PayrollAreaId);

        // Legacy line 695: "dd/MM/yyyy", rendered invariantly so the voucher text does not change with the
        // caller's culture.
        var voucherDate = periodEnd.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        // Legacy lines 782, 856: the Description prefix uses a different format again.
        var descriptionPeriod = periodEnd.ToString("MMM-yyyy", CultureInfo.InvariantCulture);

        var rows = input.ReportType switch
        {
            PayrollJVCustom035ReportType.MissingAccounts => BuildMissingAccountsRows(lines),
            PayrollJVCustom035ReportType.EmployeeWiseBreakDown =>
                BuildBreakDownRows(lines, payrollArea, voucherDate, descriptionPeriod),
            _ => BuildMainFormatRows(lines, payrollArea, voucherDate, descriptionPeriod)
        };

        return rows.AsQueryable();
    }

    /// <summary>
    /// The preamble every type shares: the filtered <c>SalaryRegisterDetail</c> set (legacy lines 568-609)
    /// LEFT JOINED to the effective-dated <c>EmployeeWageAccount</c> mapping (lines 640-690).
    /// </summary>
    private async Task<List<JoinedLine>> LoadJoinedLinesAsync(
        GetPayrollJVCustom035ReportInput input,
        DateTime periodEnd)
    {
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var payrollAreaId = input.PayrollAreaId;
        var runType = input.PayrollRunType;

        var registerQuery = _salaryRegisterDetailRepository.GetAll()
            // Legacy lines 568-576. NOTE: unlike Custom 045, Provision wage types are NOT included.
            .Where(d => d.SalaryRegister.ForPeriod == periodEnd
                        && d.SalaryRegister.ForPeriod == d.SalaryRegister.InPeriod
                        && d.WageType.IncludeInFIPosting
                        && (d.WageType.BillingType == BillingType.Earning
                            || d.WageType.BillingType == BillingType.Deduction))
            // Legacy lines 579-587.
            .WhereIf(runType.HasValue, d => d.SalaryRegister.PayrollRunType == runType.Value)
            // Legacy lines 589-598: an org assignment CURRENT AS OF the period end in that payroll area.
            .WhereIf(payrollAreaId.HasValue, d => _orgAssignmentRepository.GetAll()
                .Any(o => o.EmployeeId == d.SalaryRegister.EmployeeId
                          && o.StartDate <= periodEnd
                          && o.EndDate >= periodEnd
                          && o.PayrollAreaId == payrollAreaId.Value))
            // Legacy lines 600-607.
            .WhereIf(employeeCodes.Count > 0, d => employeeCodes.Contains(d.SalaryRegister.Employee.Code))
            // DELIBERATE DIVERGENCE: legacy never called its own ApplyRowLevelSecurity.
            .WhereIf(rlsEmployeeIds.Count > 0, d => rlsEmployeeIds.Contains(d.SalaryRegister.EmployeeId));

        // Legacy lines 610-627.
        var registerLines = await registerQuery
            .Select(d => new
            {
                d.SalaryRegister.ForPeriod,
                d.SalaryRegister.EmployeeId,
                EmployeeCode = d.SalaryRegister.Employee.Code,
                d.WageTypeId,
                WageTypeCode = d.WageType.Code,
                WageTypeName = d.WageType.NameEn,
                d.WageType.BillingType,
                d.Amount
            })
            .ToListAsync();

        // Legacy lines 640-649: the mapping current as of the period end, narrowed by the same employee
        // codes.
        var mappings = await _employeeWageAccountRepository.GetAll()
            .Where(w => w.StartDate <= periodEnd && w.EndDate >= periodEnd)
            .WhereIf(employeeCodes.Count > 0, w => employeeCodes.Contains(w.Employee.Code))
            .Select(w => new WageAccountRef
            {
                EmployeeId = w.EmployeeId,
                WageTypeId = w.WageTypeId,
                AccountCode = w.AccountInfo.Code,
                AccountName = w.AccountInfo.NameEn,
                AccountTypeName = w.AccountInfo.AccountType == null ? null : w.AccountInfo.AccountType.NameEn,
                OffsetAccountCode = w.OffsetAccountInfo == null ? null : w.OffsetAccountInfo.Code,
                OffsetAccountName = w.OffsetAccountInfo == null ? null : w.OffsetAccountInfo.NameEn,
                // Legacy slot names on this control (lines 745-751).
                DepartmentCode = w.CostElement1 == null ? null : w.CostElement1.Code,
                StaffCode = w.CostElement2 == null ? null : w.CostElement2.Code,
                DepartmentAllocationCode = w.CostElement3 == null ? null : w.CostElement3.Code,
                SectorCode = w.CostElement7 == null ? null : w.CostElement7.Code,
                CostPercentage = w.CostPercentage
            })
            .ToListAsync();

        var mappingLookup = mappings.ToLookup(m => (m.EmployeeId, m.WageTypeId));

        // Legacy lines 663-690: a LEFT join, so a register line with no mapping survives with nulls.
        var joined = new List<JoinedLine>();

        foreach (var line in registerLines)
        {
            var matches = mappingLookup[(line.EmployeeId, line.WageTypeId)].ToList();

            if (matches.Count == 0)
            {
                matches.Add(null);
            }

            foreach (var mapping in matches)
            {
                joined.Add(new JoinedLine
                {
                    ForPeriod = line.ForPeriod,
                    EmployeeId = line.EmployeeId,
                    EmployeeCode = line.EmployeeCode,
                    WageTypeId = line.WageTypeId,
                    WageTypeCode = line.WageTypeCode,
                    WageTypeName = line.WageTypeName,
                    BillingType = line.BillingType,
                    Amount = line.Amount,
                    Mapping = mapping
                });
            }
        }

        return joined;
    }

    /// <summary>
    /// Legacy lines 590-592 and 692: the payroll area is resolved ONLY when the combo was set, and it
    /// supplies the voucher's company and bank cells.
    /// </summary>
    private async Task<PayrollAreaRef> LoadPayrollAreaAsync(int? payrollAreaId)
    {
        if (!payrollAreaId.HasValue)
        {
            return null;
        }

        return await _payrollAreaRepository.GetAll()
            .Where(a => a.Id == payrollAreaId.Value)
            .Select(a => new PayrollAreaRef
            {
                CompanyCode = a.FICompany == null ? null : a.FICompany.Code,
                BankAccountCode = a.FIBank == null ? null : a.FIBank.Code,
                BankAccountTypeName = a.FIBank == null || a.FIBank.AccountType == null
                    ? null
                    : a.FIBank.AccountType.NameEn
            })
            .FirstOrDefaultAsync();
    }

    /// <summary>Legacy "Main Format" branch (lines 709-870).</summary>
    private static List<PayrollJVCustom035ReportRowDto> BuildMainFormatRows(
        List<JoinedLine> lines,
        PayrollAreaRef payrollArea,
        string voucherDate,
        string descriptionPeriod)
    {
        var rows = new List<PayrollJVCustom035ReportRowDto>();

        // Legacy lines 741-757: earnings other than net pay, grouped by account + the four cost-element
        // slots (NO employee in the key -- this sheet is a summary).
        foreach (var group in GroupByAccountAndDimensions(lines.Where(l =>
                     l.BillingType == BillingType.Earning && l.WageTypeCode != NetPayWageTypeCode)))
        {
            rows.Add(BuildVoucherRow(
                rows.Count + 1,
                MainFormatColumns,
                group,
                payrollArea,
                voucherDate,
                descriptionPeriod,
                isCreditEntry: false));
        }

        // Legacy lines 799-815. NOTE: no net-pay exclusion -- see defect 1 in the class remarks.
        foreach (var group in GroupByAccountAndDimensions(lines.Where(l =>
                     l.BillingType == BillingType.Deduction)))
        {
            rows.Add(BuildVoucherRow(
                rows.Count + 1,
                MainFormatColumns,
                group,
                payrollArea,
                voucherDate,
                descriptionPeriod,
                isCreditEntry: true));
        }

        // Legacy lines 838-869: emitted unconditionally -- see defects 1 and 2.
        rows.Add(BuildNetSalaryRow(
            rows.Count + 1,
            MainFormatColumns,
            lines,
            payrollArea,
            voucherDate,
            descriptionPeriod));

        return rows;
    }

    /// <summary>Legacy "EmployeeWise BreakDown" branch (lines 1028-1220).</summary>
    private static List<PayrollJVCustom035ReportRowDto> BuildBreakDownRows(
        List<JoinedLine> lines,
        PayrollAreaRef payrollArea,
        string voucherDate,
        string descriptionPeriod)
    {
        var rows = new List<PayrollJVCustom035ReportRowDto>();

        // Legacy lines 1069-1086: same as Main Format but with employee and wage type in the group key.
        foreach (var group in GroupByEmployeeAccountAndDimensions(lines.Where(l =>
                     l.BillingType == BillingType.Earning && l.WageTypeCode != NetPayWageTypeCode)))
        {
            var row = BuildVoucherRow(
                rows.Count + 1,
                BreakDownColumns,
                group,
                payrollArea,
                voucherDate,
                descriptionPeriod,
                isCreditEntry: false);

            row.Values[ColEmpCode] = group.EmployeeCode;
            row.Values[ColWageType] = group.WageTypeName;

            rows.Add(row);
        }

        // Legacy lines 1129-1146.
        foreach (var group in GroupByEmployeeAccountAndDimensions(lines.Where(l =>
                     l.BillingType == BillingType.Deduction)))
        {
            var row = BuildVoucherRow(
                rows.Count + 1,
                BreakDownColumns,
                group,
                payrollArea,
                voucherDate,
                descriptionPeriod,
                isCreditEntry: true);

            row.Values[ColEmpCode] = group.EmployeeCode;
            row.Values[ColWageType] = group.WageTypeName;

            rows.Add(row);
        }

        // Legacy lines 1196-1220. LEGACY DEFECT 3 REPRODUCED: no EmpCode and no WageType on this row even
        // though the whole sheet is per-employee.
        rows.Add(BuildNetSalaryRow(
            rows.Count + 1,
            BreakDownColumns,
            lines,
            payrollArea,
            voucherDate,
            descriptionPeriod));

        return rows;
    }

    /// <summary>Legacy grouping key of the Main Format loops (lines 743, 801).</summary>
    private static List<VoucherGroup> GroupByAccountAndDimensions(IEnumerable<JoinedLine> lines)
    {
        return lines
            .GroupBy(l => new
            {
                AccountCode = l.Mapping?.AccountCode,
                AccountName = l.Mapping?.AccountName,
                AccountTypeName = l.Mapping?.AccountTypeName,
                DepartmentCode = l.Mapping?.DepartmentCode,
                StaffCode = l.Mapping?.StaffCode,
                DepartmentAllocationCode = l.Mapping?.DepartmentAllocationCode,
                SectorCode = l.Mapping?.SectorCode
            })
            .Select(g => new VoucherGroup
            {
                AccountCode = g.Key.AccountCode,
                AccountName = g.Key.AccountName,
                AccountTypeName = g.Key.AccountTypeName,
                DepartmentCode = g.Key.DepartmentCode,
                StaffCode = g.Key.StaffCode,
                DepartmentAllocationCode = g.Key.DepartmentAllocationCode,
                SectorCode = g.Key.SectorCode,
                CostAmount = g.Sum(l => l.Amount)
            })
            .Where(g => g.CostAmount != 0)
            .ToList();
    }

    /// <summary>Legacy grouping key of the EmployeeWise BreakDown loops (lines 1071, 1131).</summary>
    private static List<VoucherGroup> GroupByEmployeeAccountAndDimensions(IEnumerable<JoinedLine> lines)
    {
        return lines
            .GroupBy(l => new
            {
                l.EmployeeId,
                l.EmployeeCode,
                l.WageTypeId,
                l.WageTypeName,
                AccountCode = l.Mapping?.AccountCode,
                AccountName = l.Mapping?.AccountName,
                AccountTypeName = l.Mapping?.AccountTypeName,
                DepartmentCode = l.Mapping?.DepartmentCode,
                StaffCode = l.Mapping?.StaffCode,
                DepartmentAllocationCode = l.Mapping?.DepartmentAllocationCode,
                SectorCode = l.Mapping?.SectorCode
            })
            .Select(g => new VoucherGroup
            {
                EmployeeCode = g.Key.EmployeeCode,
                WageTypeName = g.Key.WageTypeName,
                AccountCode = g.Key.AccountCode,
                AccountName = g.Key.AccountName,
                AccountTypeName = g.Key.AccountTypeName,
                DepartmentCode = g.Key.DepartmentCode,
                StaffCode = g.Key.StaffCode,
                DepartmentAllocationCode = g.Key.DepartmentAllocationCode,
                SectorCode = g.Key.SectorCode,
                CostAmount = g.Sum(l => l.Amount)
            })
            .Where(g => g.CostAmount != 0)
            .ToList();
    }

    /// <summary>
    /// Legacy Debit Entry (lines 759-797) and Credit Entry (lines 817-836) -- identical except for which
    /// side the sign maps to.
    /// </summary>
    private static PayrollJVCustom035ReportRowDto BuildVoucherRow(
        int serialNumber,
        (string Name, bool IsNumeric)[] columns,
        VoucherGroup group,
        PayrollAreaRef payrollArea,
        string voucherDate,
        string descriptionPeriod,
        bool isCreditEntry)
    {
        var row = NewRow(serialNumber, columns);

        row.Values[ColVoucher] = string.Empty;
        row.Values[ColDate] = voucherDate;
        row.Values[ColCompanyAccounts] = payrollArea?.CompanyCode ?? string.Empty;
        row.Values[ColAccountType] = group.AccountTypeName ?? string.Empty;

        // LEGACY DEFECT 4 REPRODUCED: fixed-shape concatenation with a "---" filler (lines 771-777).
        row.Values[ColAccount] = (group.AccountCode ?? string.Empty)
                                 + "-" + (group.DepartmentCode ?? string.Empty)
                                 + "-" + (group.StaffCode ?? string.Empty)
                                 + "-" + (group.DepartmentAllocationCode ?? string.Empty)
                                 + "---"
                                 + "-" + (group.SectorCode ?? string.Empty);

        row.Values[ColDescription] = descriptionPeriod + "-" + (group.AccountName ?? string.Empty);

        if (isCreditEntry)
        {
            // Legacy lines 829-830.
            row.Amounts[ColDebit] = group.CostAmount < 0 ? -group.CostAmount : 0;
            row.Amounts[ColCredit] = group.CostAmount >= 0 ? group.CostAmount : 0;
        }
        else
        {
            // Legacy lines 784-785.
            row.Amounts[ColDebit] = group.CostAmount >= 0 ? group.CostAmount : 0;
            row.Amounts[ColCredit] = group.CostAmount < 0 ? -group.CostAmount : 0;
        }

        // Legacy lines 786-789/833-836: the offset columns are declared and always left blank.
        row.Values[ColOffsetAccountType] = string.Empty;
        row.Values[ColOffsetAccount] = string.Empty;
        row.Values[ColPaymentStatus] = PaymentStatusNone;
        row.Values[ColAccountDetails] = string.Empty;

        return row;
    }

    /// <summary>Legacy "Net Salary Entry (AccountType = Bank)" (lines 838-869 / 1196-1220).</summary>
    private static PayrollJVCustom035ReportRowDto BuildNetSalaryRow(
        int serialNumber,
        (string Name, bool IsNumeric)[] columns,
        List<JoinedLine> lines,
        PayrollAreaRef payrollArea,
        string voucherDate,
        string descriptionPeriod)
    {
        // Legacy line 843: the sum of wage type 9000 over the SAME joined set -- so a net-pay line missing
        // from the FI-posting set silently yields zero. See defect 1.
        var netPayAmount = lines.Where(l => l.WageTypeCode == NetPayWageTypeCode).Sum(l => l.Amount);

        var row = NewRow(serialNumber, columns);

        row.Values[ColVoucher] = string.Empty;
        row.Values[ColDate] = voucherDate;
        row.Values[ColCompanyAccounts] = payrollArea?.CompanyCode ?? string.Empty;
        row.Values[ColAccountType] = payrollArea?.BankAccountTypeName ?? string.Empty;
        row.Values[ColAccount] = payrollArea?.BankAccountCode ?? string.Empty;
        // Legacy line 852: this row's Description is the bare period, with no account name appended.
        row.Values[ColDescription] = descriptionPeriod;

        row.Amounts[ColDebit] = netPayAmount < 0 ? -netPayAmount : 0;
        row.Amounts[ColCredit] = netPayAmount >= 0 ? netPayAmount : 0;

        row.Values[ColOffsetAccountType] = string.Empty;
        row.Values[ColOffsetAccount] = string.Empty;
        row.Values[ColPaymentStatus] = PaymentStatusNone;
        row.Values[ColAccountDetails] = string.Empty;

        return row;
    }

    /// <summary>Legacy "Missing Accounts" branch (lines 961-1026).</summary>
    private static List<PayrollJVCustom035ReportRowDto> BuildMissingAccountsRows(List<JoinedLine> lines)
    {
        var rows = new List<PayrollJVCustom035ReportRowDto>();

        // Legacy line 995: the joined lines whose left join found no mapping. As on its siblings, that
        // means the cost-element columns below are always blank by construction.
        foreach (var line in lines.Where(l => l.Mapping == null))
        {
            var row = NewRow(rows.Count + 1, MissingAccountsColumns);

            row.Values[ColEmpCode] = line.EmployeeCode;
            // Legacy typed this column DateTime; carried as an invariant ISO date here.
            row.Values[MaForPeriod] = line.ForPeriod.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            row.Values[ColWageType] = line.WageTypeName;

            row.Values[MaAccountInfoCode] = string.Empty;
            row.Values[MaAccountInfoName] = string.Empty;
            row.Values[MaOffsetAccountInfoCode] = string.Empty;
            row.Values[MaOffsetAccountInfoName] = string.Empty;

            row.Values[MaDepartments] = string.Empty;
            row.Values[MaStaffCodes] = string.Empty;
            row.Values[MaDepartmentAllocation] = string.Empty;
            row.Values[MaSector] = string.Empty;

            row.Amounts[MaCostPercentage] = 0;
            row.Amounts[MaAmount] = line.Amount;

            rows.Add(row);
        }

        return rows;
    }

    private static PayrollJVCustom035ReportRowDto NewRow(
        int serialNumber,
        (string Name, bool IsNumeric)[] columns)
    {
        var row = new PayrollJVCustom035ReportRowDto
        {
            Id = serialNumber,
            SerialNumber = serialNumber
        };

        foreach (var (name, isNumeric) in columns)
        {
            if (isNumeric)
            {
                row.Amounts[name] = 0;
            }
            else
            {
                row.Values[name] = string.Empty;
            }
        }

        return row;
    }

    private static (string Name, bool IsNumeric)[] GetColumnDefinitions(PayrollJVCustom035ReportType type)
    {
        return type switch
        {
            PayrollJVCustom035ReportType.MissingAccounts => MissingAccountsColumns,
            PayrollJVCustom035ReportType.EmployeeWiseBreakDown => BreakDownColumns,
            _ => MainFormatColumns
        };
    }

    /// <summary>
    /// Legacy <c>GetTable()</c>: Month/Year default to today's and resolve to the LAST day of that month;
    /// an unparseable pair fell back to 1900-01-31.
    /// </summary>
    private DateTime ResolvePeriodEnd(GetPayrollJVCustom035ReportInput input)
    {
        var month = input.Month ?? Today.Month;
        var year = input.Year ?? Today.Year;

        if (month < 1 || month > 12 || year < 1 || year > 9999)
        {
            return new DateTime(1900, 1, 31);
        }

        return new DateTime(year, month, DateTime.DaysInMonth(year, month));
    }

    /// <summary>
    /// Legacy <c>GetParameterStr</c>: Payroll Area, Run Type, Month, Year, Type, Employee Code.
    /// </summary>
    private string BuildJvParameterString(GetPayrollJVCustom035ReportInput input)
    {
        var parts = new List<string>();

        if (input.PayrollAreaId.HasValue)
        {
            parts.Add("Payroll Area : " + input.PayrollAreaId.Value);
        }

        if (input.PayrollRunType.HasValue)
        {
            parts.Add("Run Type : " + input.PayrollRunType.Value);
        }

        parts.Add("Month : " + (input.Month ?? Today.Month));
        parts.Add("Year : " + (input.Year ?? Today.Year));
        parts.Add("Type : " + input.ReportType);

        if (!input.EmployeeCode.IsNullOrWhiteSpace())
        {
            parts.Add("Employee Code : " + input.EmployeeCode);
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// The exported column set of the SELECTED type, in legacy declaration order. Every header goes through
    /// <c>FromLiteralHeader</c> -- these captions are reconciled against the legacy file, not localization
    /// keys, so none of them can ever render as an unresolved [Key].
    /// </summary>
    private static IReadOnlyList<GridReportColumn<PayrollJVCustom035ReportRowDto>> GetExportColumns(
        PayrollJVCustom035ReportType type)
    {
        var columns = new List<GridReportColumn<PayrollJVCustom035ReportRowDto>>
        {
            GridReportColumn<PayrollJVCustom035ReportRowDto>.FromLiteralHeader("Oid", r => r.SerialNumber)
        };

        foreach (var (name, isNumeric) in GetColumnDefinitions(type))
        {
            var columnName = name;

            columns.Add(isNumeric
                ? GridReportColumn<PayrollJVCustom035ReportRowDto>.FromLiteralHeader(
                    columnName,
                    r => r.Amounts.TryGetValue(columnName, out var amount) ? amount : 0)
                : GridReportColumn<PayrollJVCustom035ReportRowDto>.FromLiteralHeader(
                    columnName,
                    r => r.Values.TryGetValue(columnName, out var value) ? value : string.Empty));
        }

        return columns;
    }

    private class JoinedLine
    {
        public DateTime ForPeriod { get; set; }

        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public int WageTypeId { get; set; }

        public string WageTypeCode { get; set; }

        public string WageTypeName { get; set; }

        public BillingType BillingType { get; set; }

        public double Amount { get; set; }

        /// <summary>Null when the left join found no <c>EmployeeWageAccount</c> for the period.</summary>
        public WageAccountRef Mapping { get; set; }
    }

    private class WageAccountRef
    {
        public int EmployeeId { get; set; }

        public int WageTypeId { get; set; }

        public string AccountCode { get; set; }

        public string AccountName { get; set; }

        public string AccountTypeName { get; set; }

        public string OffsetAccountCode { get; set; }

        public string OffsetAccountName { get; set; }

        public string DepartmentCode { get; set; }

        public string StaffCode { get; set; }

        public string DepartmentAllocationCode { get; set; }

        public string SectorCode { get; set; }

        public double CostPercentage { get; set; }
    }

    private class PayrollAreaRef
    {
        public string CompanyCode { get; set; }

        public string BankAccountCode { get; set; }

        public string BankAccountTypeName { get; set; }
    }

    private class VoucherGroup
    {
        public string EmployeeCode { get; set; }

        public string WageTypeName { get; set; }

        public string AccountCode { get; set; }

        public string AccountName { get; set; }

        public string AccountTypeName { get; set; }

        public string DepartmentCode { get; set; }

        public string StaffCode { get; set; }

        public string DepartmentAllocationCode { get; set; }

        public string SectorCode { get; set; }

        public double CostAmount { get; set; }
    }
}
