using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using ClosedXML.Excel;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.GridReports.Employees;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement.BankFiles;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PayrollManagement.Exporting;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Bank salary-transfer file generation. Ported from the LIVE legacy screen
/// <c>HCM10.Web/UserControls/PayrollManagement/uscBankFile.ascx.cs</c> (<c>btnGenerate_Click</c>, lines
/// 110-2511, plus the helper region 2513-3460). The 28 format branches are line-for-line those of
/// <c>uscBankFile2.ascx.cs</c>, which they were first ported from; only the screen around them differs.
///
/// LAYERING: this service does the DATA GATHERING only -- it resolves every employee's bank, sponsor,
/// on-hold state and net pay into flat <see cref="BankFilePayeeRow"/> instances, then hands them to the pure
/// <see cref="BankFileGenerator"/> domain service in <c>HCMv3.Core</c>. All 28 layouts live in Core and are
/// unit-testable without a database.
///
/// WHAT THE LIVE SCREEN DOES, AND WHERE IT IS HERE:
/// <list type="bullet">
/// <item>Period Current / Other (<c>:2528-2564</c>) -- <see cref="IBankFilePeriodResolver"/>, worked out on the
/// server so a browser's time zone cannot move the end date a day.</item>
/// <item>Payment Type ALL / BANK / CASH (<c>:219-238</c>) -- <see cref="BankFilePaymentTypeFilter"/>.</item>
/// <item>Sponsor (A/C) listed for the chosen sponsor only (<c>BindSponsorBank :2608</c>) --
/// <see cref="GetBankFileSponsorBanks"/>.</item>
/// <item>Run history with a zipped copy of every file (<c>SysService.AddExecutionLog</c>, the "View Log"
/// popup <c>:3367-3460</c>) -- <see cref="BankFileExecutionLogWriter"/>, <see cref="GetBankFileLogs"/>.</item>
/// <item>Salary registers locked when the file is made (<c>LockSalaryRegisters :2513</c>) --
/// <see cref="LockRegistersAsync"/>.</item>
/// </list>
///
/// DELIBERATE DEVIATIONS FROM LEGACY:
/// 1. The file is returned base64 in the DTO rather than streamed with <c>Response.WriteFile</c>, and the copy
///    legacy wrote to <c>~/Contents/BankFile</c> goes to the tenant's file store instead.
/// 2. SPREADSHEETS ARE .xlsx, NOT .xls (ClosedXML, not DevExpress <c>WriteXls</c>). Cell VALUES and column
///    order are identical; only the container differs.
/// 3. CULTURE PINNED TO INVARIANT for every numeric field (see <c>BankFileFieldFormat</c>).
/// 4. Failed runs are logged (legacy logged successes only) -- see <see cref="BankFileExecutionLogWriter"/>.
/// 5. With "separate file per employee bank", every selected register is locked; legacy locked only those of
///    the LAST bank its loop visited (its <c>allSalRegList</c> is reassigned per bank, <c>:409</c>), and that
///    order is whatever the database returned -- not something that can be reproduced.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_BankFiles)]
public class BankFilesAppService : HCMv3AppServiceBase, IBankFilesAppService
{
    private const string WageTypeNetPayCode = "9000";

    /// <summary>Legacy <c>OrgService.OrgUT_Company</c>: the org-unit TYPE NAME that marks a company.</summary>
    private const string CompanyOrgUnitTypeName = "Company";

    private const string DateFormat = "dd/MM/yyyy";

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
    private readonly IRepository<OrganizationUnitType> _organizationUnitTypeRepository;
    private readonly IRepository<ExecutionLog> _executionLogRepository;
    private readonly IRepository<AppAttachment> _attachmentRepository;
    private readonly ISysSettingsAccessor _sysSettings;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;
    private readonly IBankFilePeriodResolver _periodResolver;
    private readonly BankFileExecutionLogWriter _logWriter;
    private readonly IAttachmentManager _attachmentManager;
    private readonly ITempFileCacheManager _tempFileCacheManager;
    private readonly IBankFileLogExcelExporter _logExcelExporter;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;
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
        IRepository<OrganizationUnitType> organizationUnitTypeRepository,
        IRepository<ExecutionLog> executionLogRepository,
        IRepository<AppAttachment> attachmentRepository,
        ISysSettingsAccessor sysSettings,
        IEmployeePayrollAreaResolver payrollAreaResolver,
        IBankFilePeriodResolver periodResolver,
        BankFileExecutionLogWriter logWriter,
        IAttachmentManager attachmentManager,
        ITempFileCacheManager tempFileCacheManager,
        IBankFileLogExcelExporter logExcelExporter,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
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
        _organizationUnitTypeRepository = organizationUnitTypeRepository;
        _executionLogRepository = executionLogRepository;
        _attachmentRepository = attachmentRepository;
        _sysSettings = sysSettings;
        _payrollAreaResolver = payrollAreaResolver;
        _periodResolver = periodResolver;
        _logWriter = logWriter;
        _attachmentManager = attachmentManager;
        _tempFileCacheManager = tempFileCacheManager;
        _logExcelExporter = logExcelExporter;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    #region Lookups and period

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

        output.SponsorBanks = (await _sponsorBankRepository.GetAll()
                .Where(r => r.PaymentBank != null)
                .OrderBy(r => r.PaymentBank.Code)
                .Select(r => new { r.Id, r.SponsorId, r.PaymentBank.Code, r.AccountNumber })
                .ToListAsync())
            .Select(r => SponsorBankDto(r.Id, r.SponsorId, r.Code, r.AccountNumber))
            .ToList();

        // Legacy BindMosalId: Sponsor.Select(r => r.MOSALID).Distinct().
        output.MosalIds = (await _sponsorRepository.GetAll().Select(r => r.MOSALID).Distinct().ToListAsync())
            .OrderBy(m => m)
            .ToList();

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

    /// <summary>
    /// Legacy <c>BindSponsorBank</c> (<c>:2608-2627</c>): every account of the chosen sponsor, as
    /// "PaymentBank.Code (AccountNumber)". No date filter -- legacy had none.
    /// </summary>
    public async Task<List<BankFileSponsorBankDto>> GetBankFileSponsorBanks(EntityDto input)
    {
        return (await _sponsorBankRepository.GetAll()
                .Where(r => r.SponsorId == input.Id)
                .OrderBy(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.SponsorId,
                    Code = r.PaymentBank != null ? r.PaymentBank.Code : null,
                    r.AccountNumber
                })
                .ToListAsync())
            .Select(r => SponsorBankDto(r.Id, r.SponsorId, r.Code, r.AccountNumber))
            .ToList();
    }

    [HttpPost]
    public async Task<BankFilePeriodDto> GetBankFilePeriod(GetBankFilePeriodInput input)
    {
        try
        {
            var period = await _periodResolver.ResolveAsync(
                input.PeriodType, input.PayrollAreaId, input.PayrollMonth, input.PayrollYear);

            return new BankFilePeriodDto
            {
                HasPeriod = true,
                PeriodType = period.PeriodType,
                PayrollMonth = period.PayrollMonth,
                PayrollYear = period.PayrollYear,
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                StartDateText = period.StartDate.ToString(DateFormat, CultureInfo.InvariantCulture),
                EndDateText = period.EndDate.ToString(DateFormat, CultureInfo.InvariantCulture)
            };
        }
        catch (UserFriendlyException ex)
        {
            // Legacy's period callback swallowed the error and left the boxes as they were.
            return new BankFilePeriodDto
            {
                HasPeriod = false,
                PeriodType = input.PeriodType,
                PayrollMonth = input.PayrollMonth,
                PayrollYear = input.PayrollYear,
                Message = ex.Message
            };
        }
    }

    #endregion

    #region Generate

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_BankFiles_Generate)]
    public async Task<BankFileOutput> GenerateBankFile(GenerateBankFileInput input)
    {
        var userName = await CurrentUserNameAsync();
        var parameters = await BuildRunParametersAsync(input);

        try
        {
            return await GenerateCoreAsync(input, userName, parameters);
        }
        catch (Exception ex)
        {
            await _logWriter.WriteFailedAsync(userName, parameters.ToLegacyString(), ex.Message);
            throw;
        }
    }

    private async Task<BankFileOutput> GenerateCoreAsync(
        GenerateBankFileInput input, string userName, BankFileRunParameters parameters)
    {
        ValidateInput(input);

        var payerBank = await _bankRepository.GetAll()
            .Include(r => r.BankFile)
            .FirstOrDefaultAsync(r => r.Id == input.PaymentBankId);

        if (payerBank == null)
        {
            throw new UserFriendlyException(L("BankFilePaymentBankNotSelected"));
        }

        var period = await ResolveRunPeriodAsync(input);
        var endDate = period.EndDate;
        parameters.PeriodEndText = endDate.ToString(DateFormat, CultureInfo.InvariantCulture);

        await ValidatePayrollControlRecordAsync(input, endDate);

        // ---- 1. Employee filter chain (legacy :112-327) -------------------------------------------------
        var employeeIds = await ResolveEmployeeIdsAsync(input, endDate, payerBank);
        if (employeeIds.Count == 0)
        {
            throw new UserFriendlyException(L("BankFileNoEmployeesMatchedFilters"));
        }

        // ---- 2. Salary registers + net pay (legacy :329-375) --------------------------------------------
        var payrollSettings = await _sysSettings.GetPayrollSettingsAsync();
        var minAmount = payrollSettings?.BankFileMinAmount ?? 0d;

        var registers = await QueryRegistersAsync(input, period, employeeIds);
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

        // ---- 4. Split per payee bank (legacy :378-416) --------------------------------------------------
        var payeeBankIdsPresent = rows
            .Where(r => r.EmployeeBankId.HasValue)
            .Select(r => r.EmployeeBankId.Value)
            .Distinct()
            .ToList();

        var splitBankFiles = input.SplitBankFiles && payeeBankIdsPresent.Count > 1;

        var produced = new List<RenderedFile>();

        if (!splitBankFiles)
        {
            var doc = Generate(context, payerBank.BankFile?.NameEn);
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

                var doc = Generate(subContext, assigned);
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

        BankFileOutput output;
        string storedName;
        byte[] storedZip;

        if (produced.Count == 1)
        {
            var single = produced[0];
            output = new BankFileOutput
            {
                FileName = single.FileName,
                Base64Content = Convert.ToBase64String(single.Content),
                ContentType = single.ContentType,
                FormatCode = single.FormatCode,
                RecordCount = single.Rows,
                TotalAmount = single.Total
            };

            // Legacy :2899-2920 -- the single file is zipped for the log, named after the file.
            storedName = Path.GetFileNameWithoutExtension(single.FileName) + ".zip";
            storedZip = Zip(new List<(string, byte[])> { (single.FileName, single.Content) });
        }
        else
        {
            var zipName = "BankFile_" + payerBank.Code + "_" + endDate.ToString("MMyyyy") + "_" +
                          context.GeneratedOn.ToString("ddMMyyyy_HHmmssff") + ".zip";
            var zip = Zip(produced.Select(p => (p.FileName, p.Content)).ToList());

            output = new BankFileOutput
            {
                FileName = zipName,
                Base64Content = Convert.ToBase64String(zip),
                ContentType = "application/zip",
                FormatCode = string.Join(",", produced.Select(p => p.FormatCode).Distinct()),
                RecordCount = produced.Sum(p => p.Rows),
                TotalAmount = produced.Sum(p => p.Total)
            };

            storedName = zipName;
            storedZip = zip;
        }

        // ---- 5. Lock + log, committed together with the run (legacy :2513-2526, SysService.cs:1048) -----
        output.RegistersLocked = await LockRegistersAsync(
            registerIds, payrollSettings?.LockSalaryRegisterOnPayrollExit == true);

        var (logId, _) = await _logWriter.WriteSuccessAsync(
            userName, parameters.ToLegacyString(), output.RecordCount, storedName, storedZip);

        output.ExecutionLogId = logId;
        output.StartDateText = period.StartDate.ToString(DateFormat, CultureInfo.InvariantCulture);
        output.EndDateText = parameters.PeriodEndText;

        return output;
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

    /// <summary>Legacy's "File Format not found for Bank Code" (:2496-2499) as a message, not a crash.</summary>
    private BankFileDocument Generate(BankFileContext context, string assignedFormatCode)
    {
        try
        {
            return _generator.Generate(context, assignedFormatCode);
        }
        catch (BankFileFormatNotFoundException ex)
        {
            throw new UserFriendlyException(L("BankFileFormatNotAssignedForBank", ex.PayerBankCode));
        }
    }

    /// <summary>
    /// Legacy <c>LockSalaryRegisters</c> (<c>uscBankFile.ascx.cs:2513-2526</c>): when
    /// <c>LockSalaryRegisterOnPayrollExit</c> is on, every selected register still unlocked is locked -- regular
    /// AND off-cycle, whatever the run type picked, and including those the file then leaves out (on hold,
    /// below the minimum amount, no account): legacy passed its whole <c>allSalRegList</c>.
    ///
    /// <para>Exiting payroll (<c>PayrollControlRecordsAppService.LockRegistersForStateAsync</c>) locks with the
    /// same rule -- only unlocked rows, only to <c>true</c> -- so whichever runs first, the other finds nothing
    /// left to do: no double lock, no conflict, and neither ever unlocks.</para>
    /// </summary>
    /// <returns>How many registers this run locked.</returns>
    private async Task<int> LockRegistersAsync(List<int> registerIds, bool lockOnPayrollExit)
    {
        if (!lockOnPayrollExit || registerIds.Count == 0)
        {
            return 0;
        }

        var unlocked = await _salaryRegisterRepository.GetAll()
            .Where(r => registerIds.Contains(r.Id) && !r.Locked)
            .ToListAsync();

        foreach (var register in unlocked)
        {
            register.Locked = true;
            await _salaryRegisterRepository.UpdateAsync(register);
        }

        return unlocked.Count;
    }

    private async Task<BankFilePeriod> ResolveRunPeriodAsync(GenerateBankFileInput input)
    {
        if (input.PeriodType.HasValue)
        {
            return await _periodResolver.ResolveAsync(input.PeriodType.Value, input.PayrollAreaId, input.Month, input.Year);
        }

        // A server-side caller (Go-Live Setup) passes its dates directly; they never crossed a time zone.
        return new BankFilePeriod
        {
            PeriodType = BankFilePeriodType.Other,
            PayrollMonth = input.Month,
            PayrollYear = input.Year,
            StartDate = input.StartDate.Date,
            EndDate = input.EndDate.Date
        };
    }

    private async Task<string> CurrentUserNameAsync()
    {
        if (!AbpSession.UserId.HasValue)
        {
            return null;
        }

        var user = await UserManager.FindByIdAsync(AbpSession.UserId.Value.ToString());
        return user?.UserName;
    }

    /// <summary>The display texts legacy put in the log's parameter string (<c>GetParameterString :2813</c>).</summary>
    private async Task<BankFileRunParameters> BuildRunParametersAsync(GenerateBankFileInput input)
    {
        var parameters = new BankFileRunParameters
        {
            PayrollRunTypeText = string.IsNullOrWhiteSpace(input.PayrollRunType)
                ? null
                : BankFileRunParameters.RunTypeText(input.PayrollRunType),
            PaymentTypeText = BankFileRunParameters.PaymentTypeTextOf(input.PaymentType),
            PeriodTypeText = BankFileRunParameters.PeriodTypeTextOf(input.PeriodType),
            MosalIdText = input.MosalId?.ToString(CultureInfo.InvariantCulture),
            EmployeeCodes = input.EmployeeCodes,
            SplitBankFiles = input.SplitBankFiles,
            PeriodEndText = !input.PeriodType.HasValue && input.EndDate != default
                ? input.EndDate.ToString(DateFormat, CultureInfo.InvariantCulture)
                : null
        };

        if (input.PayrollAreaId.HasValue)
        {
            parameters.PayrollAreaName = await _payrollAreaRepository.GetAll()
                .Where(r => r.Id == input.PayrollAreaId.Value)
                .Select(r => r.Name)
                .FirstOrDefaultAsync();
        }

        var payerBank = await _bankRepository.GetAll()
            .Where(r => r.Id == input.PaymentBankId)
            .Select(r => new { r.Code, r.NameEn })
            .FirstOrDefaultAsync();
        if (payerBank != null)
        {
            parameters.PaymentBankText = payerBank.Code + "-" + payerBank.NameEn;
        }

        if (input.EmployeeBankId.HasValue)
        {
            var employeeBank = await _bankRepository.GetAll()
                .Where(r => r.Id == input.EmployeeBankId.Value)
                .Select(r => new { r.Code, r.NameEn })
                .FirstOrDefaultAsync();
            if (employeeBank != null)
            {
                parameters.EmployeeBankText = employeeBank.Code + "-" + employeeBank.NameEn;
            }
        }

        if (input.SponsorId.HasValue)
        {
            parameters.SponsorText = await _sponsorRepository.GetAll()
                .Where(r => r.Id == input.SponsorId.Value)
                .Select(r => r.NameEn)
                .FirstOrDefaultAsync();
        }

        if (input.SponsorBankId.HasValue)
        {
            var sponsorBank = await _sponsorBankRepository.GetAll()
                .Where(r => r.Id == input.SponsorBankId.Value)
                .Select(r => new { Code = r.PaymentBank != null ? r.PaymentBank.Code : null, r.AccountNumber })
                .FirstOrDefaultAsync();
            if (sponsorBank != null)
            {
                parameters.SponsorBankText = SponsorBankText(sponsorBank.Code, sponsorBank.AccountNumber);
            }
        }

        return parameters;
    }

    #endregion

    #region Run log

    [HttpPost]
    public async Task<BankFileLogsOutput> GetBankFileLogs(GetBankFileLogsInput input)
    {
        var all = BankFileLogs();

        var allCount = await all.CountAsync();
        var successfulCount = await all.CountAsync(r => r.ExecutionStatus == ExecutionLogStatus.SUCCESS);

        var query = FilterLogs(all, input.StatusFilter);
        var totalCount = await query.CountAsync();

        var rows = await SortLogs(query, input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new BankFileLogsOutput
        {
            TotalCount = totalCount,
            Items = await ToLogDtosAsync(rows),
            AllCount = allCount,
            SuccessfulCount = successfulCount,
            FailedCount = allCount - successfulCount
        };
    }

    [HttpPost]
    public async Task<FileDto> ExportBankFileLogs(GetBankFileLogsInput input)
    {
        var rows = await SortLogs(FilterLogs(BankFileLogs(), input.StatusFilter), input.Sorting).ToListAsync();

        return await _logExcelExporter.ExportToFile(await ToLogDtosAsync(rows));
    }

    /// <summary>Downloading the copy shows salaries, so it needs the right to make the file in the first place.</summary>
    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_BankFiles_Generate)]
    public async Task<FileDto> GetBankFileLogDownload(EntityDto input)
    {
        var log = await BankFileLogs().FirstOrDefaultAsync(r => r.Id == input.Id);
        if (log == null)
        {
            throw new UserFriendlyException(L("BankFileLogHasNoFile"));
        }

        var attachment = (await _attachmentManager.GetListAsync(
                BankFileExecutionLogWriter.AttachmentEntityType, log.Id.ToString()))
            .FirstOrDefault();

        if (attachment == null)
        {
            throw new UserFriendlyException(L("BankFileLogHasNoFile"));
        }

        var content = await _attachmentManager.ReadContentAsync(attachment);
        if (content == null)
        {
            throw new UserFriendlyException(L("AttachmentFileMissing"));
        }

        var file = new FileDto(attachment.FileName, attachment.ContentType);
        _tempFileCacheManager.SetFile(file.FileToken, content);

        return file;
    }

    private IQueryable<ExecutionLog> BankFileLogs() =>
        _executionLogRepository.GetAll().Where(r => r.Module == ExecutionLogModule.BankFile);

    /// <summary>Legacy <c>SetLogGridDataSource</c> (<c>:3419-3445</c>): Failed is everything not SUCCESS.</summary>
    private static IQueryable<ExecutionLog> FilterLogs(IQueryable<ExecutionLog> query, BankFileLogFilter filter) =>
        filter switch
        {
            BankFileLogFilter.Successful => query.Where(r => r.ExecutionStatus == ExecutionLogStatus.SUCCESS),
            BankFileLogFilter.Failed => query.Where(r => r.ExecutionStatus != ExecutionLogStatus.SUCCESS),
            _ => query
        };

    /// <summary>Newest first, as legacy (<c>OrderByDescending(r => r.ActionDateTime)</c>), unless the grid asks otherwise.</summary>
    private static IQueryable<ExecutionLog> SortLogs(IQueryable<ExecutionLog> query, string sorting)
    {
        if (sorting.IsNullOrWhiteSpace())
        {
            return query.OrderByDescending(r => r.ActionDateTime).ThenByDescending(r => r.Id);
        }

        // The grid sorts by DTO field names; "succeeded" is a projection of the entity's status.
        return query.OrderBy(sorting.Replace("succeeded", "executionStatus", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<BankFileLogDto>> ToLogDtosAsync(List<ExecutionLog> rows)
    {
        var ids = rows.Select(r => r.Id.ToString()).ToList();

        var files = (await _attachmentRepository.GetAll()
                .Where(a => a.EntityType == BankFileExecutionLogWriter.AttachmentEntityType && ids.Contains(a.EntityId))
                .OrderBy(a => a.Id)
                .Select(a => new { a.Id, a.EntityId, a.FileName })
                .ToListAsync())
            .GroupBy(a => a.EntityId)
            .ToDictionary(g => g.Key, g => g.First());

        return rows.Select(r =>
        {
            files.TryGetValue(r.Id.ToString(), out var file);
            return new BankFileLogDto
            {
                Id = r.Id,
                UserName = r.UserName,
                ActionDateTime = r.ActionDateTime,
                ExecutionStatus = r.ExecutionStatus,
                Succeeded = r.ExecutionStatus == ExecutionLogStatus.SUCCESS,
                Parameters = r.Parameters,
                TotalRecords = r.TotalRecords,
                Successfull = r.Successfull,
                Failed = r.Failed,
                AttachmentId = file?.Id,
                FileName = file?.FileName,
                HasFile = file != null
            };
        }).ToList();
    }

    #endregion

    #region Filtering

    private void ValidateInput(GenerateBankFileInput input)
    {
        // Legacy PerformValidation() :2738-2747
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

    /// <summary>Legacy <c>PerformValidation()</c> :2749-2777 -- block export past a closed control record.</summary>
    private async Task ValidatePayrollControlRecordAsync(GenerateBankFileInput input, DateTime endDate)
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

        if (maxDate < endDate)
        {
            throw new UserFriendlyException(
                L("BankFilePayrollControlRecordClosed", maxDate.ToString(DateFormat, CultureInfo.InvariantCulture)));
        }
    }

    private async Task<List<int>> ResolveEmployeeIdsAsync(GenerateBankFileInput input, DateTime endDate, Bank payerBank)
    {
        // Legacy :120-126 -- optional comma-separated employee code list.
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

        // Legacy :129-143 -- EmployeeOrgAssignment.PayrollArea == the chosen area, on EndDate. Read through
        // IEmployeePayrollAreaResolver, as the payroll engine does: the assignment's own PayrollAreaId first,
        // and -- for assignments made before that column existed -- the org unit's payroll area.
        if (input.PayrollAreaId.HasValue)
        {
            var areaName = await _payrollAreaRepository.GetAll()
                .Where(r => r.Id == input.PayrollAreaId.Value)
                .Select(r => r.Name)
                .FirstOrDefaultAsync();

            var inArea = areaName == null
                ? new HashSet<int>()
                : new HashSet<int>(await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(areaName, endDate));

            ids = ids.Where(inArea.Contains).ToList();
        }

        // Legacy :146-161 -- Sponsor.
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

        // Legacy :205-216 -- MOSAL ID.
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

        // Legacy :219-238 -- Payment Type radio. ALL still narrows to employees who HAVE an employee-bank row
        // in force on EndDate; BANK / CASH also need its payment type.
        var bankQuery = _employeeBankRepository.GetAll()
            .Where(r => ids.Contains(r.EmployeeId) && r.StartDate <= endDate && r.EndDate >= endDate);

        var paymentType = BankFilePaymentTypeFilter.FilterOf(input.PaymentType);
        if (paymentType.HasValue)
        {
            bankQuery = bankQuery.Where(r => r.PaymentType == paymentType.Value);
        }

        ids = await bankQuery.Select(r => r.EmployeeId).Distinct().ToListAsync();

        // Legacy :241-254 -- Employee Bank.
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

        // Legacy :257-287 -- PayerBank. When ANY EmployeeBank row in the period names this payer bank,
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

        // Legacy :290-326 -- Organization Unit filter. Its combo is Visible="false" and BindOrganizationUnit is
        // commented out of Setup on the live screen, so legacy never applied it. Refused rather than ignored.
        if (input.OrganizationUnitId.HasValue)
        {
            throw new UserFriendlyException(L("BankFileOrganizationUnitFilterNotSupported"));
        }

        return ids;
    }

    private async Task<List<SalaryRegister>> QueryRegistersAsync(
        GenerateBankFileInput input, BankFilePeriod period, List<int> employeeIds)
    {
        var endDate = period.EndDate;
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
            // Legacy :345-357 + GetCutoffDate() :2780-2796.
            var cutoff = await GetCutoffDateAsync(period);
            query = query.Where(r => r.PayrollRunType == PayrollRunType.Regular
                                     || (r.PayrollRunType == PayrollRunType.OffCycle
                                         && r.OffCyclePaymentDate >= cutoff));
        }

        // Legacy :340-342 -- the register must have details, and must NOT have a zero-amount 9000 line.
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

    /// <summary>
    /// Legacy <c>GetCutoffDate()</c> (<c>:2780-2796</c>): the cutoff day in the PAYROLL month and year
    /// (<c>txtMonth</c>/<c>txtyear</c>), as if they were calendar ones -- even for a fiscal payroll period where
    /// payroll month 1 is not January. Reproduced as-is.
    /// </summary>
    private async Task<DateTime> GetCutoffDateAsync(BankFilePeriod period)
    {
        var settings = await _sysSettings.GetPayrollSettingsAsync();
        var cutoffDay = settings?.OffcycleBankFileCutoff ?? 0;
        cutoffDay = cutoffDay == 0 ? 1 : cutoffDay;

        var daysInMonth = DateTime.DaysInMonth(period.PayrollYear, period.PayrollMonth);
        cutoffDay = daysInMonth < cutoffDay ? daysInMonth : cutoffDay;

        return new DateTime(period.PayrollYear, period.PayrollMonth, cutoffDay);
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

        // Only GBK-T1 prints the company (legacy :1340), and only the GB/GBK branch can choose GBK-T1.
        var companyNames = payerBank.Code == "GB" || payerBank.Code == "GBK"
            ? await ResolveCurrentCompanyNamesAsync(employeeIds)
            : new Dictionary<int, string>();

        var rows = new List<BankFilePayeeRow>();

        foreach (var emp in employees)
        {
            bankByEmployee.TryGetValue(emp.Id, out var empBank);
            sponsorByEmployee.TryGetValue(emp.Id, out var empSponsor);
            companyNames.TryGetValue(emp.Id, out var companyName);

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
                CurrentCompanyNameEn = companyName
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

    /// <summary>
    /// Legacy <c>Employee.CurrentCompany.NameEn</c> (HCM10.Module Employee.cs:1404): the org-unit TYPE named
    /// "Company" gives a level code (OrgLevel1..7); the employee's org assignment in force TODAY -- legacy's
    /// <c>CurrentOrgAssignment</c> uses <c>DateTime.Now.Date</c>, not the period end -- gives the unit at that
    /// level (<c>OrgService.GetCurrentOrgUnit</c>). In HCMv3 that unit is the ancestor-or-self of the assignment's
    /// unit classified at the level, read through the same <see cref="EmployeeOrgLevelNameResolver"/> the grid
    /// reports use for OrgLevel1..7. No "Company" type, or one at Level0/8/9, gives no name -- as legacy.
    /// </summary>
    private async Task<Dictionary<int, string>> ResolveCurrentCompanyNamesAsync(List<int> employeeIds)
    {
        var result = new Dictionary<int, string>();

        var companyLevelCode = await _organizationUnitTypeRepository.GetAll()
            .Where(t => t.NameEn == CompanyOrgUnitTypeName)
            .OrderBy(t => t.Id)
            .Select(t => t.Code)
            .FirstOrDefaultAsync();

        var levelIndex = Array.IndexOf(EmployeeOrgLevelNameResolver.LevelCodes, companyLevelCode);
        if (levelIndex < 0)
        {
            return result;
        }

        var today = Clock.Now.Date;
        var units = (await _employeeOrgAssignmentRepository.GetAll()
                .Where(a => employeeIds.Contains(a.EmployeeId)
                            && a.StartDate <= today && a.EndDate >= today
                            && a.OrganizationUnitId != null)
                .OrderBy(a => a.Id)
                .Select(a => new { a.EmployeeId, a.OrganizationUnitId })
                .ToListAsync())
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First().OrganizationUnitId);

        if (units.Count == 0)
        {
            return result;
        }

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(useArabicNames: false);

        foreach (var (employeeId, unitId) in units)
        {
            var name = lookup.GetLevelNames(unitId)[levelIndex];
            if (!string.IsNullOrEmpty(name))
            {
                result[employeeId] = name;
            }
        }

        return result;
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
    /// Legacy (<c>:894-899</c>) picks the first <c>OrganizationBankInfo</c> whose org unit's TYPE NAME equals
    /// <c>OrgService.OrgUT_Company</c> and whose AccountNo is non-empty -- and notably does NOT filter by the
    /// payer bank. This port approximates "company level" as the TOP level (Code "Level0"), falling back to any
    /// non-empty AccountNo. DEVIATION -- kept as it was (Go-Live Setup relies on the fallback); reported.
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

    private static BankFileSponsorBankDto SponsorBankDto(int id, int sponsorId, string bankCode, string accountNumber) =>
        new()
        {
            Id = id,
            SponsorId = sponsorId,
            Code = bankCode,
            AccountNumber = accountNumber,
            // Legacy renders "NBK (1234567890)" (:2617) and then re-parses that string (:171-191).
            DisplayText = SponsorBankText(bankCode, accountNumber)
        };

    private static string SponsorBankText(string bankCode, string accountNumber) =>
        (bankCode ?? string.Empty) + " (" + (accountNumber ?? string.Empty) + ")";

    #endregion

    #region Rendering

    private sealed record RenderedFile(
        string FileName, byte[] Content, string ContentType, string FormatCode, int Rows, double Total);

    /// <summary>
    /// Bytes + the figures shown after a run. Count and total are the format's own when it states them -- for a
    /// text file, exactly what its trailer says -- otherwise the grid's rows and the sum of its amount column.
    /// </summary>
    private static RenderedFile Render(
        BankFileDocument doc, string payerBankCode, DateTime endDate, DateTime generatedOn, string payeeBankCode)
    {
        var stem = "BankFile_" + payerBankCode + "_" + endDate.ToString("MMyyyy") + "_" +
                   generatedOn.ToString("ddMMyyyy_HHmmssff") +
                   (payeeBankCode != null ? "_" + payeeBankCode : string.Empty);

        if (doc.Kind == BankFileDocumentKind.Text)
        {
            return new RenderedFile(
                stem + ".txt",
                Encoding.UTF8.GetBytes(doc.TextContent ?? string.Empty),
                "text/plain",
                doc.FormatCode,
                doc.RecordCount ?? CountTextDetailLines(doc.TextContent),
                doc.TotalAmount ?? 0d);
        }

        return new RenderedFile(
            stem + ".xlsx",
            RenderXlsx(doc),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            doc.FormatCode,
            doc.RecordCount ?? doc.Rows.Count,
            doc.TotalAmount ?? SumAmountColumn(doc));
    }

    private static double SumAmountColumn(BankFileDocument doc)
    {
        var amountColumn = doc.Columns.FindIndex(c =>
            c.IndexOf("amount", StringComparison.OrdinalIgnoreCase) >= 0);

        var total = 0d;
        if (amountColumn >= 0)
        {
            foreach (var row in doc.Rows)
            {
                if (row.Count > amountColumn && row[amountColumn] is double d)
                {
                    total += d;
                }
            }
        }

        return total;
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
    /// <c>uscBankFile.ascx.cs:290-326</c> -- Organization Unit filter walking <c>Employee.OrgLevel1..OrgLevel7</c>.
    /// On the LIVE screen its combo is <c>Visible="false"</c> and <c>BindOrganizationUnit()</c> is commented out of
    /// <c>Setup</c>, so legacy never applied it either; the input is refused with a message instead of ignored.
    /// </summary>
    public const string OrganizationUnitFilter = "uscBankFile.ascx.cs:290-326";

    /// <summary>
    /// <c>uscBankFile.ascx.cs:894-899</c> (and the other CBK text branches) -- the company account is the
    /// <c>OrganizationBankInfo</c> of an org unit whose TYPE NAME is "Company". This port still picks the Level0
    /// unit's account, falling back to any account, because Go-Live Setup saves the company account on the unit
    /// most of the area's employees sit in, whatever its type.
    /// </summary>
    public const string CompanyBankAccount = "uscBankFile.ascx.cs:894-899";
}
