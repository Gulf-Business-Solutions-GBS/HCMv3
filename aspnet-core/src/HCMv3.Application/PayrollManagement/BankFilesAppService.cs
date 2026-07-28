using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using ClosedXML.Excel;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement.BankFiles;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings.Services;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Bank salary-transfer file generation. Ported from legacy
/// <c>HCM10.Web/UserControls/PayrollManagement/uscBankFile2.ascx.cs</c> (<c>btnGenerate_Click</c>,
/// lines 104-2511, plus the helper region 2515-3315).
///
/// LAYERING: this service does the DATA GATHERING only -- it resolves every employee's bank, sponsor,
/// on-hold state and net pay into flat <see cref="BankFilePayeeRow"/> instances, then hands them to the pure
/// <see cref="BankFileGenerator"/> domain service in <c>HCMv3.Core</c>. All 28 layouts live in Core and are
/// unit-testable without a database.
///
/// DELIBERATE DEVIATIONS FROM LEGACY (each also listed in the wave report):
/// 1. NO SERVER-SIDE FILES. Legacy wrote to <c>~/Contents/BankFile</c> and streamed with
///    <c>Response.WriteFile</c>; we return base64 in the DTO, per the <c>ReportPdfOutput</c> convention.
/// 2. SPREADSHEETS ARE .xlsx, NOT .xls. Legacy used DevExpress <c>ASPxGridViewExporter.WriteXls</c> (BIFF8);
///    we use ClosedXML (OpenXML). Cell VALUES and column order are identical; only the container differs.
/// 3. NO EXECUTION LOG ROW. Legacy called <c>SysService.AddExecutionLog(..., Module.BankFile, ...)</c> with
///    the zipped payload attached. HCMv3's execution-log equivalent is owned by another wave; NOT written
///    here. REPORTED, not silently dropped.
/// 4. NO SALARY-REGISTER LOCKING. Legacy's <c>LockSalaryRegisters()</c> (<c>:2515-2529</c>) flips
///    <c>SalaryRegister.Locked</c> when <c>ManagePayrollSettings.LockSalaryRegisterOnPayrollExit</c> is on.
///    That is a WRITE performed as a side effect of a download, and it belongs with the payroll-exit flow a
///    sibling agent owns in <c>Core/PayrollManagement/Services/</c> (which this wave must not touch).
///    NOT ported here -- see <see cref="BankFileNotPortedNotes"/>.
/// 5. CULTURE PINNED TO INVARIANT for every numeric field (see <c>BankFileFieldFormat</c>).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_BankFiles)]
public class BankFilesAppService : HCMv3AppServiceBase, IBankFilesAppService
{
    private const string WageTypeNetPayCode = "9000";

    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<SalaryRegisterDetail> _salaryRegisterDetailRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeBank> _employeeBankRepository;
    private readonly IRepository<EmployeeSponsor> _employeeSponsorRepository;
    private readonly IRepository<EmployeeOnHold> _employeeOnHoldRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly IRepository<Bank> _bankRepository;
    private readonly IRepository<BankFile> _bankFileRepository;
    private readonly IRepository<PayeeBankDetail> _payeeBankDetailRepository;
    private readonly IRepository<Sponsor> _sponsorRepository;
    private readonly IRepository<SponsorBank> _sponsorBankRepository;
    private readonly IRepository<PayrollArea> _payrollAreaRepository;
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly IRepository<OrganizationBankInfo> _organizationBankInfoRepository;
    private readonly IRepository<OrgUnitProfile> _orgUnitProfileRepository;
    private readonly ISysSettingsAccessor _sysSettings;
    private readonly BankFileGenerator _generator = new();

    public BankFilesAppService(
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<SalaryRegisterDetail> salaryRegisterDetailRepository,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeBank> employeeBankRepository,
        IRepository<EmployeeSponsor> employeeSponsorRepository,
        IRepository<EmployeeOnHold> employeeOnHoldRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        IRepository<Bank> bankRepository,
        IRepository<BankFile> bankFileRepository,
        IRepository<PayeeBankDetail> payeeBankDetailRepository,
        IRepository<Sponsor> sponsorRepository,
        IRepository<SponsorBank> sponsorBankRepository,
        IRepository<PayrollArea> payrollAreaRepository,
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IRepository<OrganizationBankInfo> organizationBankInfoRepository,
        IRepository<OrgUnitProfile> orgUnitProfileRepository,
        ISysSettingsAccessor sysSettings)
    {
        _salaryRegisterRepository = salaryRegisterRepository;
        _salaryRegisterDetailRepository = salaryRegisterDetailRepository;
        _employeeRepository = employeeRepository;
        _employeeBankRepository = employeeBankRepository;
        _employeeSponsorRepository = employeeSponsorRepository;
        _employeeOnHoldRepository = employeeOnHoldRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _bankRepository = bankRepository;
        _bankFileRepository = bankFileRepository;
        _payeeBankDetailRepository = payeeBankDetailRepository;
        _sponsorRepository = sponsorRepository;
        _sponsorBankRepository = sponsorBankRepository;
        _payrollAreaRepository = payrollAreaRepository;
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _organizationBankInfoRepository = organizationBankInfoRepository;
        _orgUnitProfileRepository = orgUnitProfileRepository;
        _sysSettings = sysSettings;
    }

    public async Task<BankFileLookupsOutput> GetBankFileLookups()
    {
        var output = new BankFileLookupsOutput();

        output.PayrollAreas = await _payrollAreaRepository.GetAll()
            .OrderBy(r => r.Code)
            .Select(r => new BankFileLookupItemDto { Id = r.Id, Code = r.Code, DisplayText = r.Name })
            .ToListAsync();

        output.Sponsors = await _sponsorRepository.GetAll()
            .OrderBy(r => r.NameEn)
            .Select(r => new BankFileLookupItemDto { Id = r.Id, Code = r.Code, DisplayText = r.NameEn })
            .ToListAsync();

        output.SponsorBanks = await _sponsorBankRepository.GetAll()
            .Include(r => r.PaymentBank)
            .Where(r => r.PaymentBank != null)
            .OrderBy(r => r.PaymentBank.Code)
            .Select(r => new BankFileSponsorBankDto
            {
                Id = r.Id,
                Code = r.PaymentBank.Code,
                AccountNumber = r.AccountNumber,
                // Legacy renders "NBK (1234567890)" and then re-parses that string (:171-191).
                DisplayText = r.PaymentBank.Code + " (" + r.AccountNumber + ")"
            })
            .ToListAsync();

        var banks = await _bankRepository.GetAll()
            .OrderBy(r => r.Code)
            .Select(r => new BankFileLookupItemDto { Id = r.Id, Code = r.Code, DisplayText = r.NameEn })
            .ToListAsync();

        output.PaymentBanks = banks;
        output.EmployeeBanks = banks.Select(b => new BankFileLookupItemDto
        {
            Id = b.Id,
            Code = b.Code,
            DisplayText = b.DisplayText
        }).ToList();

        var seededCodes = await _bankFileRepository.GetAll().Select(r => r.Code).ToListAsync();
        var seeded = new HashSet<string>(seededCodes.Where(c => c != null));

        output.Formats = BankFileFormatCodes.All.Select(code => new BankFileFormatDto
        {
            Code = code,
            NameEn = code,
            PayerBankCode = PayerBankCodeOf(code),
            IsTextFormat = code.Contains("-T"),
            IsSeeded = seeded.Contains(code)
        }).ToList();

        return output;
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_BankFiles_Generate)]
    public async Task<BankFileOutput> GenerateBankFile(GenerateBankFileInput input)
    {
        ValidateInput(input);

        var payerBank = await _bankRepository.GetAll()
            .Include(r => r.BankFile)
            .FirstOrDefaultAsync(r => r.Id == input.PaymentBankId);

        if (payerBank == null)
        {
            throw new UserFriendlyException(L("BankFilePaymentBankNotSelected"));
        }

        await ValidatePayrollControlRecordAsync(input);

        var endDate = input.EndDate.Date;

        // ---- 1. Employee filter chain (legacy :117-290) -------------------------------------------------
        var employeeIds = await ResolveEmployeeIdsAsync(input, endDate, payerBank);
        if (employeeIds.Count == 0)
        {
            throw new UserFriendlyException(L("BankFileNoEmployeesMatchedFilters"));
        }

        // ---- 2. Salary registers + net pay (legacy :327-376) --------------------------------------------
        var payrollSettings = await _sysSettings.GetPayrollSettingsAsync();
        var minAmount = payrollSettings?.BankFileMinAmount ?? 0d;

        var registers = await QueryRegistersAsync(input, endDate, employeeIds);
        if (registers.Count == 0)
        {
            throw new UserFriendlyException(L("BankFileNoSalaryRegistersFound"));
        }

        var registerIds = registers.Select(r => r.Id).ToList();

        var netPayQuery = _salaryRegisterDetailRepository.GetAll()
            .Where(r => registerIds.Contains(r.SalaryRegisterId)
                        && r.WageType != null
                        && r.WageType.Code == WageTypeNetPayCode
                        && r.Amount != 0);

        if (minAmount != 0)
        {
            netPayQuery = netPayQuery.Where(r => r.Amount >= minAmount);
        }

        var netPayRows = await netPayQuery
            .Select(r => new { r.SalaryRegisterId, r.Amount })
            .ToListAsync();

        var registerToEmployee = registers.ToDictionary(r => r.Id, r => r.EmployeeId);
        var amountByEmployee = new Dictionary<int, double>();
        foreach (var d in netPayRows)
        {
            if (!registerToEmployee.TryGetValue(d.SalaryRegisterId, out var empId))
            {
                continue;
            }

            amountByEmployee[empId] = amountByEmployee.TryGetValue(empId, out var running)
                ? running + d.Amount
                : d.Amount;
        }

        var payeeEmployeeIds = registers.Select(r => r.EmployeeId).Distinct().ToList();
        var registerCountByEmployee = registers
            .GroupBy(r => r.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Count());

        // ---- 3. Flatten every payee row (legacy's per-row lookups) --------------------------------------
        var rows = await BuildPayeeRowsAsync(
            payeeEmployeeIds, endDate, payerBank, amountByEmployee, registerCountByEmployee);

        var grandTotal = rows.Sum(r => r.Amount);

        var context = new BankFileContext
        {
            EndDate = endDate,
            GeneratedOn = Clock.Now,
            PayerBankCode = payerBank.Code,
            PayerBankExternalCode = payerBank.ExternalCode,
            PayerBankCompanyCode = payerBank.CompanyCode,
            SponsorBankAccountNumber = await ResolveSponsorBankAccountNumberAsync(input),
            OrganizationBankAccountNo = await ResolveOrganizationBankAccountNoAsync(payerBank.Id),
            SelectedSponsorMosalId = await ResolveSelectedSponsorMosalIdAsync(input),
            EmployeeBankFilterSelected = input.EmployeeBankId.HasValue,
            GrandTotalAmountAllBanks = grandTotal,
            Rows = rows
        };

        // ---- 4. Split per payee bank (legacy :381-416) --------------------------------------------------
        var payeeBankIdsPresent = rows
            .Where(r => r.EmployeeBankId.HasValue)
            .Select(r => r.EmployeeBankId.Value)
            .Distinct()
            .ToList();

        var splitBankFiles = input.SplitBankFiles && payeeBankIdsPresent.Count > 1;

        var produced = new List<(string FileName, byte[] Content, string ContentType, string FormatCode, int Rows, double Total)>();

        if (!splitBankFiles)
        {
            var assigned = payerBank.BankFile?.NameEn;
            var doc = _generator.Generate(context, assigned);
            if (doc != null)
            {
                produced.Add(Render(doc, payerBank.Code, endDate, context.GeneratedOn, null));
            }
        }
        else
        {
            var payeeFormatByBankId = await _payeeBankDetailRepository.GetAll()
                .Include(r => r.BankFile)
                .Where(r => r.BankId == payerBank.Id && r.PayeeBankId != null)
                .Select(r => new { PayeeBankId = r.PayeeBankId.Value, FormatName = r.BankFile.NameEn })
                .ToListAsync();

            var payeeBankCodeById = await _bankRepository.GetAll()
                .Where(r => payeeBankIdsPresent.Contains(r.Id))
                .Select(r => new { r.Id, r.Code })
                .ToDictionaryAsync(r => r.Id, r => r.Code);

            foreach (var payeeBankId in payeeBankIdsPresent)
            {
                var subContext = CloneContextForPayeeBank(context, payeeBankId);

                var assigned = payeeFormatByBankId.FirstOrDefault(f => f.PayeeBankId == payeeBankId)?.FormatName
                               ?? payerBank.BankFile?.NameEn;

                var doc = _generator.Generate(subContext, assigned);
                if (doc == null)
                {
                    continue;
                }

                payeeBankCodeById.TryGetValue(payeeBankId, out var payeeCode);
                produced.Add(Render(doc, payerBank.Code, endDate, context.GeneratedOn, payeeCode));
            }
        }

        if (produced.Count == 0)
        {
            // Legacy KIB/WRB with no format assigned fall through silently and produce nothing.
            throw new UserFriendlyException(L("BankFileFormatNotAssignedForBank", payerBank.Code));
        }

        if (produced.Count == 1)
        {
            var single = produced[0];
            return new BankFileOutput
            {
                FileName = single.FileName,
                Base64Content = Convert.ToBase64String(single.Content),
                ContentType = single.ContentType,
                FormatCode = single.FormatCode,
                RecordCount = single.Rows,
                TotalAmount = single.Total
            };
        }

        var zipName = "BankFile_" + payerBank.Code + "_" + endDate.ToString("MMyyyy") + "_" +
                      context.GeneratedOn.ToString("ddMMyyyy_HHmmssff") + ".zip";

        return new BankFileOutput
        {
            FileName = zipName,
            Base64Content = Convert.ToBase64String(Zip(produced.Select(p => (p.FileName, p.Content)).ToList())),
            ContentType = "application/zip",
            FormatCode = string.Join(",", produced.Select(p => p.FormatCode).Distinct()),
            RecordCount = produced.Sum(p => p.Rows),
            TotalAmount = produced.Sum(p => p.Total)
        };
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_BankFiles_Generate)]
    public async Task SeedBankFileFormats()
    {
        var existing = await _bankFileRepository.GetAll().Select(r => r.Code).ToListAsync();
        var present = new HashSet<string>(existing.Where(c => c != null));

        foreach (var code in BankFileFormatCodes.All.Where(c => !present.Contains(c)))
        {
            await _bankFileRepository.InsertAsync(new BankFile
            {
                Code = code,
                NameEn = code,
                NameAr = code,
                TenantId = AbpSession.GetTenantId()
            });
        }
    }

    #region Filtering

    private void ValidateInput(GenerateBankFileInput input)
    {
        // Legacy PerformValidation() :2745-2755
        if (!input.PayrollAreaId.HasValue && !input.SponsorId.HasValue && !input.MosalId.HasValue &&
            string.IsNullOrWhiteSpace(input.EmployeeCodes))
        {
            throw new UserFriendlyException(L("BankFileNoPayrollAreaSponsorMosalIdOrEmpCode"));
        }

        if (string.IsNullOrWhiteSpace(input.PayrollRunType))
        {
            throw new UserFriendlyException(L("BankFilePayrollRunTypeNotSelected"));
        }
    }

    /// <summary>Legacy <c>PerformValidation()</c> :2756-2785 -- block export past a closed control record.</summary>
    private async Task ValidatePayrollControlRecordAsync(GenerateBankFileInput input)
    {
        if (!input.PayrollAreaId.HasValue)
        {
            return;
        }

        var record = await _payrollControlRecordRepository.GetAll()
            .FirstOrDefaultAsync(r => r.PayrollAreaId == input.PayrollAreaId.Value);

        if (record == null)
        {
            throw new UserFriendlyException(L("BankFilePayrollControlRecordNotFound"));
        }

        var fiSettings = await _sysSettings.GetFIPostingSettingsAsync();
        if (fiSettings == null || !fiSettings.FiPostingAfterPayrollExit)
        {
            return;
        }

        DateTime maxDate = default;
        if (!record.PeriodClosed)
        {
            maxDate = record.CalendarMonthStart.AddDays(-1);
        }
        else if (!record.PeriodLocked)
        {
            maxDate = record.CalendarMonthEnd;
        }

        if (maxDate < input.EndDate.Date)
        {
            throw new UserFriendlyException(
                L("BankFilePayrollControlRecordClosed", maxDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));
        }
    }

    private async Task<List<int>> ResolveEmployeeIdsAsync(GenerateBankFileInput input, DateTime endDate, Bank payerBank)
    {
        // Legacy :118-124 -- optional comma-separated employee code list.
        IQueryable<int> employeeIds;
        if (!string.IsNullOrWhiteSpace(input.EmployeeCodes))
        {
            var codes = input.EmployeeCodes.Split(',').Select(c => c.Trim()).Where(c => c.Length > 0).ToList();
            employeeIds = _employeeRepository.GetAll().Where(r => codes.Contains(r.Code)).Select(r => r.Id);
        }
        else
        {
            employeeIds = _employeeRepository.GetAll().Select(r => r.Id);
        }

        var ids = await employeeIds.ToListAsync();

        // Legacy :127-142 -- Payroll Area via EmployeeOrgAssignment effective on EndDate.
        //
        // DEVIATION (forced): HCMv3's EmployeeOrgAssignment carries NO PayrollArea FK -- a known, documented
        // porting gap (see TimeEngineService.Employee.cs:268-282 remarks, which hit the same wall). We route
        // through the ORG UNIT instead: assignment effective on EndDate -> OrgUnitProfile.PayrollAreaId,
        // which is the same workaround CurrencyService.RoundAsync uses (CurrencyService.cs:59-71).
        // Behaviour matches legacy whenever payroll area is org-unit-derived, which is the normal setup.
        if (input.PayrollAreaId.HasValue)
        {
            var orgUnitIdsForArea = await _orgUnitProfileRepository.GetAll()
                .Where(p => p.PayrollAreaId == input.PayrollAreaId.Value)
                .Select(p => p.OrganizationUnitId)
                .Distinct()
                .ToListAsync();

            ids = await _employeeOrgAssignmentRepository.GetAll()
                .Where(r => ids.Contains(r.EmployeeId)
                            && r.StartDate <= endDate && r.EndDate >= endDate
                            && r.OrganizationUnitId != null
                            && orgUnitIdsForArea.Contains(r.OrganizationUnitId.Value))
                .Select(r => r.EmployeeId)
                .Distinct()
                .ToListAsync();
        }

        // Legacy :145-159 -- Sponsor.
        if (input.SponsorId.HasValue)
        {
            ids = await _employeeSponsorRepository.GetAll()
                .Where(r => ids.Contains(r.EmployeeId)
                            && r.StartDate <= endDate && r.EndDate >= endDate
                            && r.SponsorId == input.SponsorId.Value)
                .Select(r => r.EmployeeId)
                .Distinct()
                .ToListAsync();
        }

        // Legacy :199-210 -- MOSAL ID.
        if (input.MosalId.HasValue)
        {
            ids = await _employeeSponsorRepository.GetAll()
                .Where(r => ids.Contains(r.EmployeeId)
                            && r.StartDate <= endDate && r.EndDate >= endDate
                            && r.Sponsor != null && r.Sponsor.MOSALID == input.MosalId.Value)
                .Select(r => r.EmployeeId)
                .Distinct()
                .ToListAsync();
        }

        // Legacy :213-240 -- Payment Type. NOTE: even with no payment type chosen, legacy still narrows to
        // employees who HAVE an effective EmployeeBank row, which is what makes this filter mandatory.
        var bankQuery = _employeeBankRepository.GetAll()
            .Where(r => ids.Contains(r.EmployeeId) && r.StartDate <= endDate && r.EndDate >= endDate);

        if (input.PaymentType.HasValue)
        {
            bankQuery = bankQuery.Where(r => r.PaymentType == input.PaymentType.Value);
        }

        ids = await bankQuery.Select(r => r.EmployeeId).Distinct().ToListAsync();

        // Legacy :243-256 -- Employee Bank.
        if (input.EmployeeBankId.HasValue)
        {
            ids = await _employeeBankRepository.GetAll()
                .Where(r => ids.Contains(r.EmployeeId)
                            && r.BankId == input.EmployeeBankId.Value
                            && r.StartDate <= endDate && r.EndDate >= endDate)
                .Select(r => r.EmployeeId)
                .Distinct()
                .ToListAsync();
        }

        // Legacy :259-289 -- PayerBank. When ANY EmployeeBank row in the period names this payer bank,
        // restrict to those employees; otherwise restrict to employees with NO payer bank set.
        var payerBankAssigned = await _employeeBankRepository.GetAll()
            .AnyAsync(r => r.PayerBankId == payerBank.Id && r.StartDate <= endDate && r.EndDate >= endDate);

        ids = await _employeeBankRepository.GetAll()
            .Where(r => ids.Contains(r.EmployeeId)
                        && r.StartDate <= endDate && r.EndDate >= endDate
                        && (payerBankAssigned ? r.PayerBankId == payerBank.Id : r.PayerBankId == null))
            .Select(r => r.EmployeeId)
            .Distinct()
            .ToListAsync();

        // Legacy :292-325 -- Organization Unit filter.
        // NOT PORTED: legacy walks Employee.OrgLevel1..OrgLevel7 computed properties, which are Wave B in
        // HCMv3 (Employee.cs remarks: "CurrentCompany/CurrentDepartment/etc. computed properties are Wave B").
        // Rejected loudly rather than silently ignored -- see BankFileNotPortedNotes.
        if (input.OrganizationUnitId.HasValue)
        {
            throw new UserFriendlyException(L("BankFileOrganizationUnitFilterNotSupported"));
        }

        return ids;
    }

    private async Task<List<SalaryRegister>> QueryRegistersAsync(
        GenerateBankFileInput input, DateTime endDate, List<int> employeeIds)
    {
        var query = _salaryRegisterRepository.GetAll()
            .Where(r => employeeIds.Contains(r.EmployeeId)
                        && r.ForPeriod == endDate
                        && r.InPeriod == endDate);

        if (input.PayrollRunType == "R" || input.PayrollRunType == "O")
        {
            var runType = input.PayrollRunType == "R" ? PayrollRunType.Regular : PayrollRunType.OffCycle;
            query = query.Where(r => r.PayrollRunType == runType);
        }
        else
        {
            // Legacy :344-353 + GetCutoffDate() :2787-2803.
            var cutoff = await GetCutoffDateAsync(input);
            query = query.Where(r => r.PayrollRunType == PayrollRunType.Regular
                                     || (r.PayrollRunType == PayrollRunType.OffCycle
                                         && r.OffCyclePaymentDate >= cutoff));
        }

        // Legacy :338-340 -- the register must have details, and must NOT have a zero-amount 9000 line.
        var registers = await query.Select(r => new { r.Id, r.EmployeeId }).ToListAsync();
        var candidateIds = registers.Select(r => r.Id).ToList();

        var withAnyDetail = await _salaryRegisterDetailRepository.GetAll()
            .Where(r => candidateIds.Contains(r.SalaryRegisterId))
            .Select(r => r.SalaryRegisterId)
            .Distinct()
            .ToListAsync();

        var withZeroNetPay = await _salaryRegisterDetailRepository.GetAll()
            .Where(r => candidateIds.Contains(r.SalaryRegisterId)
                        && r.WageType != null && r.WageType.Code == WageTypeNetPayCode && r.Amount == 0)
            .Select(r => r.SalaryRegisterId)
            .Distinct()
            .ToListAsync();

        var hasDetail = new HashSet<int>(withAnyDetail);
        var hasZero = new HashSet<int>(withZeroNetPay);

        return registers
            .Where(r => hasDetail.Contains(r.Id) && !hasZero.Contains(r.Id))
            .Select(r => new SalaryRegister { Id = r.Id, EmployeeId = r.EmployeeId })
            .ToList();
    }

    private async Task<DateTime> GetCutoffDateAsync(GenerateBankFileInput input)
    {
        var settings = await _sysSettings.GetPayrollSettingsAsync();
        var cutoffDay = settings?.OffcycleBankFileCutoff ?? 0;
        cutoffDay = cutoffDay == 0 ? 1 : cutoffDay;

        var daysInMonth = DateTime.DaysInMonth(input.Year, input.Month);
        cutoffDay = daysInMonth < cutoffDay ? daysInMonth : cutoffDay;

        return new DateTime(input.Year, input.Month, cutoffDay);
    }

    #endregion

    #region Row building

    private async Task<List<BankFilePayeeRow>> BuildPayeeRowsAsync(
        List<int> employeeIds,
        DateTime endDate,
        Bank payerBank,
        Dictionary<int, double> amountByEmployee,
        Dictionary<int, int> registerCountByEmployee)
    {
        var employees = await _employeeRepository.GetAll()
            .Where(r => employeeIds.Contains(r.Id))
            .Select(r => new { r.Id, r.Code, r.FullNameEn, r.CivilID })
            .OrderBy(r => r.Code)
            .ToListAsync();

        // Legacy takes FirstOrDefault of the EmployeeBank rows effective on EndDate (:469-471).
        var employeeBanks = await _employeeBankRepository.GetAll()
            .Include(r => r.Bank)
            .Where(r => employeeIds.Contains(r.EmployeeId) && r.StartDate <= endDate && r.EndDate >= endDate)
            .ToListAsync();

        var bankByEmployee = employeeBanks
            .GroupBy(r => r.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First());

        var sponsors = await _employeeSponsorRepository.GetAll()
            .Include(r => r.Sponsor)
            .Where(r => employeeIds.Contains(r.EmployeeId) && r.StartDate <= endDate && r.EndDate >= endDate)
            .ToListAsync();

        var sponsorByEmployee = sponsors
            .GroupBy(r => r.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First());

        var onHoldEmployeeIds = new HashSet<int>(await _employeeOnHoldRepository.GetAll()
            .Where(r => r.StartDate <= endDate && r.EndDate >= endDate && r.BankTransfer == OnHoldStatus.Hold)
            .Select(r => r.EmployeeId)
            .ToListAsync());

        // Legacy resolves PayeeBankDetail off the PAYER bank's aggregated collection (:601).
        var payeeDetails = await _payeeBankDetailRepository.GetAll()
            .Include(r => r.Bank)
            .Where(r => r.BankId == payerBank.Id && r.PayeeBankId != null)
            .Select(r => new { PayeeBankId = r.PayeeBankId.Value, r.BankCode, BankNameEn = r.Bank.NameEn })
            .ToListAsync();

        var payeeDetailByBankId = payeeDetails
            .GroupBy(r => r.PayeeBankId)
            .ToDictionary(g => g.Key, g => g.First());

        var rows = new List<BankFilePayeeRow>();

        foreach (var emp in employees)
        {
            bankByEmployee.TryGetValue(emp.Id, out var empBank);
            sponsorByEmployee.TryGetValue(emp.Id, out var empSponsor);

            var row = new BankFilePayeeRow
            {
                EmployeeId = emp.Id,
                EmployeeCode = emp.Code,
                FullNameEn = emp.FullNameEn,
                CivilId = emp.CivilID,
                HasBankRecord = empBank != null,
                Amount = amountByEmployee.TryGetValue(emp.Id, out var amt) ? amt : 0d,
                IsBankTransferOnHold = onHoldEmployeeIds.Contains(emp.Id),
                RegisterCount = registerCountByEmployee.TryGetValue(emp.Id, out var rc) ? rc : 1,
                HasSponsor = empSponsor != null,
                SponsorNameEn = empSponsor?.Sponsor?.NameEn,
                SponsorMosalId = empSponsor?.Sponsor?.MOSALID ?? 0,
                // NOT PORTED: legacy GBK-T1 uses Employee.CurrentCompany.NameEn (:1342), a Wave B computed
                // property on the HCMv3 Employee entity. Emitted blank; see BankFileNotPortedNotes.
                CurrentCompanyNameEn = null
            };

            if (empBank != null)
            {
                row.AccountNum = empBank.AccountNum;
                row.Iban = empBank.IBAN;
                row.PaymentType = empBank.PaymentType;
                row.BankBranch = empBank.BankBranch;
                row.EmployeeBankId = empBank.BankId;
                row.EmployeeBankCode = empBank.Bank?.Code;
                row.EmployeeBankNameEn = empBank.Bank?.NameEn;
                row.EmployeeBankSwiftCode = empBank.Bank?.SWIFTCode;
                row.EmployeeBankIsPayerBank = empBank.BankId == payerBank.Id;

                if (empBank.BankId.HasValue &&
                    payeeDetailByBankId.TryGetValue(empBank.BankId.Value, out var detail))
                {
                    row.PayeeBankCode = detail.BankCode;
                    row.PayeeBankNameEn = detail.BankNameEn;
                }
                else
                {
                    row.PayeeBankCode = string.Empty;
                    row.PayeeBankNameEn = string.Empty;
                }
            }

            rows.Add(row);
        }

        return rows;
    }

    private static BankFileContext CloneContextForPayeeBank(BankFileContext source, int payeeBankId)
    {
        return new BankFileContext
        {
            EndDate = source.EndDate,
            GeneratedOn = source.GeneratedOn,
            PayerBankCode = source.PayerBankCode,
            PayerBankExternalCode = source.PayerBankExternalCode,
            PayerBankCompanyCode = source.PayerBankCompanyCode,
            SponsorBankAccountNumber = source.SponsorBankAccountNumber,
            OrganizationBankAccountNo = source.OrganizationBankAccountNo,
            SelectedSponsorMosalId = source.SelectedSponsorMosalId,
            EmployeeBankFilterSelected = source.EmployeeBankFilterSelected,
            // Legacy never re-filters salRegDetailList when splitting, so CBK-G2's total row keeps the
            // full unsplit sum (:781).
            GrandTotalAmountAllBanks = source.GrandTotalAmountAllBanks,
            Rows = source.Rows.Where(r => r.EmployeeBankId == payeeBankId).ToList()
        };
    }

    private async Task<string> ResolveSponsorBankAccountNumberAsync(GenerateBankFileInput input)
    {
        if (!input.SponsorBankId.HasValue)
        {
            return string.Empty;
        }

        var sponsorBank = await _sponsorBankRepository.GetAll()
            .FirstOrDefaultAsync(r => r.Id == input.SponsorBankId.Value);

        return sponsorBank?.AccountNumber ?? string.Empty;
    }

    /// <summary>
    /// Legacy (<c>:896-899</c>) picks the first <c>OrganizationBankInfo</c> whose org unit's TYPE NAME equals
    /// <c>OrgService.OrgUT_Company</c> and whose AccountNo is non-empty -- and notably does NOT filter by the
    /// payer bank. HCMv3's org-unit types are coded Level0..Level9 rather than named, so we approximate
    /// "company level" as the TOP level (Code "Level0"), falling back to any non-empty AccountNo.
    /// DEVIATION -- reported.
    /// </summary>
    private async Task<string> ResolveOrganizationBankAccountNoAsync(int payerBankId)
    {
        var candidates = await _organizationBankInfoRepository.GetAll()
            .Where(r => r.AccountNo != null && r.AccountNo != "" && r.OrganizationUnitId != null)
            .Select(r => new { r.AccountNo, r.OrganizationUnitId })
            .ToListAsync();

        if (candidates.Count == 0)
        {
            return string.Empty;
        }

        // The unit's TYPE lives on the OrgUnitProfile companion row, not on OrganizationUnit itself.
        var orgUnitIds = candidates.Select(c => c.OrganizationUnitId.Value).Distinct().ToList();

        var topLevelUnitIds = new HashSet<long>(await _orgUnitProfileRepository.GetAll()
            .Where(p => orgUnitIds.Contains(p.OrganizationUnitId)
                        && p.OrganizationUnitType != null
                        && p.OrganizationUnitType.Code == "Level0")
            .Select(p => p.OrganizationUnitId)
            .ToListAsync());

        var companyLevel = candidates.FirstOrDefault(c => topLevelUnitIds.Contains(c.OrganizationUnitId.Value));

        return (companyLevel ?? candidates[0]).AccountNo;
    }

    private async Task<long?> ResolveSelectedSponsorMosalIdAsync(GenerateBankFileInput input)
    {
        if (!input.SponsorId.HasValue)
        {
            return null;
        }

        var sponsor = await _sponsorRepository.GetAll()
            .FirstOrDefaultAsync(r => r.Id == input.SponsorId.Value);

        return sponsor?.MOSALID;
    }

    #endregion

    #region Rendering

    private static (string FileName, byte[] Content, string ContentType, string FormatCode, int Rows, double Total)
        Render(BankFileDocument doc, string payerBankCode, DateTime endDate, DateTime generatedOn, string payeeBankCode)
    {
        var stem = "BankFile_" + payerBankCode + "_" + endDate.ToString("MMyyyy") + "_" +
                   generatedOn.ToString("ddMMyyyy_HHmmssff") +
                   (payeeBankCode != null ? "_" + payeeBankCode : string.Empty);

        if (doc.Kind == BankFileDocumentKind.Text)
        {
            return (stem + ".txt",
                    Encoding.UTF8.GetBytes(doc.TextContent ?? string.Empty),
                    "text/plain",
                    doc.FormatCode,
                    CountTextDetailLines(doc.TextContent),
                    0d);
        }

        var amountColumn = doc.Columns.FindIndex(c =>
            c.IndexOf("amount", StringComparison.OrdinalIgnoreCase) >= 0);

        var total = 0d;
        if (amountColumn >= 0)
        {
            foreach (var row in doc.Rows)
            {
                if (row[amountColumn] is double d)
                {
                    total += d;
                }
            }
        }

        return (stem + ".xlsx",
                RenderXlsx(doc),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                doc.FormatCode,
                doc.Rows.Count,
                total);
    }

    /// <summary>Detail lines only -- the header and trailer are excluded.</summary>
    private static int CountTextDetailLines(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return 0;
        }

        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        return Math.Max(0, lines.Length - 2);
    }

    private static byte[] RenderXlsx(BankFileDocument doc)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("BankFile");

        for (var c = 0; c < doc.Columns.Count; c++)
        {
            sheet.Cell(1, c + 1).Value = doc.Columns[c];
        }

        for (var r = 0; r < doc.Rows.Count; r++)
        {
            var row = doc.Rows[r];
            for (var c = 0; c < row.Count && c < doc.Columns.Count; c++)
            {
                var cell = sheet.Cell(r + 2, c + 1);
                switch (row[c])
                {
                    case null:
                        break;
                    case double d:
                        cell.Value = d;
                        break;
                    case int i:
                        cell.Value = i;
                        break;
                    case long l:
                        cell.Value = l;
                        break;
                    default:
                        // Explicit text so long digit strings (IBAN, civil id) keep leading zeros.
                        cell.SetValue(row[c].ToString());
                        cell.Style.NumberFormat.Format = "@";
                        break;
                }
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] Zip(List<(string FileName, byte[] Content)> files)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(file.FileName);
                using var entryStream = entry.Open();
                entryStream.Write(file.Content, 0, file.Content.Length);
            }
        }

        return stream.ToArray();
    }

    #endregion

    private static string PayerBankCodeOf(string formatCode)
    {
        var dash = formatCode.LastIndexOf('-');
        return dash > 0 ? formatCode.Substring(0, dash) : formatCode;
    }
}

/// <summary>
/// Explicit ledger of legacy behaviour deliberately NOT ported by <see cref="BankFilesAppService"/>,
/// with precise legacy pointers. Kept in source so the gap is loud rather than plausible-looking.
/// </summary>
public static class BankFileNotPortedNotes
{
    /// <summary>
    /// <c>uscBankFile2.ascx.cs:2515-2529 LockSalaryRegisters()</c> -- sets <c>SalaryRegister.Locked</c> on
    /// every exported register when <c>ManagePayrollSettings.LockSalaryRegisterOnPayrollExit</c> is true.
    /// Owned by the payroll-exit flow in <c>Core/PayrollManagement/Services/</c>, which this wave must not
    /// modify. Needs to be wired in by whoever owns that file.
    /// </summary>
    public const string SalaryRegisterLocking = "uscBankFile2.ascx.cs:2515-2529";

    /// <summary>
    /// <c>uscBankFile2.ascx.cs:2934-2951 / 3040-3056 SysService.AddExecutionLog(..., Module.BankFile, ...)</c>
    /// -- writes an execution-log row carrying the zipped payload and the parameter string built by
    /// <c>GetParameterString()</c> (<c>:2820-2862</c>). HCMv3's execution-log equivalent is another wave's.
    /// </summary>
    public const string ExecutionLog = "uscBankFile2.ascx.cs:2934-2951, 3040-3056, 2820-2862";

    /// <summary>
    /// <c>uscBankFile2.ascx.cs:292-325</c> -- Organization Unit filter walking
    /// <c>Employee.OrgLevel1..OrgLevel7</c>. Those computed properties are Wave B on HCMv3's Employee
    /// entity, so the filter throws a localized "not supported" error instead of silently ignoring it.
    /// </summary>
    public const string OrganizationUnitFilter = "uscBankFile2.ascx.cs:292-325";

    /// <summary>
    /// <c>uscBankFile2.ascx.cs:1342</c> -- GBK-T1's trailing company name reads
    /// <c>Employee.CurrentCompany.NameEn</c>, also a Wave B computed property. Emitted blank for now; the
    /// field sits at the END of the detail line, so its absence does not shift any other field.
    /// </summary>
    public const string GbkT1CompanyName = "uscBankFile2.ascx.cs:1342";
}
