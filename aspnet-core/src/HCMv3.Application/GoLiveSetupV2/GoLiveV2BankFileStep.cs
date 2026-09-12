using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Organizations;
using Abp.Runtime.Session;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GoLiveSetupV2.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement.BankFiles;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PayrollManagement.Services;
using HCMv3.PersonnelAdministration;
using Microsoft.EntityFrameworkCore;
using PayrollBankFiles = HCMv3.PayrollManagement.IBankFilesAppService;

namespace HCMv3.GoLiveSetupV2;

/// <summary>
/// Step 4, the month's salary file. The file itself comes from the Bank File screen's own service and its 28
/// ported layouts; this step only chooses what that screen leaves to an administrator -- which bank pays, in
/// which layout, with which company account -- and saves those choices where the screen reads them, so the two
/// make the same file.
///
/// <para><b>Which layouts are offered.</b> Those that work from what V2 records per employee: the bank, the
/// IBAN, and the account number where the sheet gives one. A layout that pays into account numbers rather
/// than IBANs -- CBK-T1 and T2, BUR-G1, the Burgan B-Dinar pair, NBK-G2 -- says so, and leaves out anyone
/// without one. Two are still left to the Bank File screen: CBK-T7 keys off a sponsor's bank account, and
/// GBK-T1 has one client's own values written into it.</para>
/// </summary>
public class GoLiveV2BankFileStep : AbpServiceBase, ITransientDependency
{
    /// <summary>
    /// By the bank codes the generator switches on. The first is the bank's usual layout. The Kuwait pack's
    /// "Gulf Bank" is "GB"; the generator also knows it as "GBK".
    /// </summary>
    private static readonly IReadOnlyDictionary<string, Layout[]> LayoutsByBankCode =
        new Dictionary<string, Layout[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["NBK"] = new[]
            {
                Grid(BankFileFormatCodes.NbkG1, payeeBankCodes: true, mosalId: true),
                Grid(BankFileFormatCodes.NbkG3),
                Grid(BankFileFormatCodes.NbkG2, employeeAccount: true),
            },
            ["CBK"] = new[]
            {
                Grid(BankFileFormatCodes.CbkG1),
                Grid(BankFileFormatCodes.CbkG2),
                Text(BankFileFormatCodes.CbkT3, companyAccount: true, payeeBankCodes: true),
                Text(BankFileFormatCodes.CbkT4, companyAccount: true),
                Text(BankFileFormatCodes.CbkT5, companyAccount: true, payeeBankCodes: true),
                Text(BankFileFormatCodes.CbkT6, companyAccount: true, payeeBankCodes: true),
                Text(BankFileFormatCodes.CbkT1, companyAccount: true, employeeAccount: true),
                Text(BankFileFormatCodes.CbkT2, companyAccount: true, employeeAccount: true),
            },
            ["GB"] = new[] { Grid(BankFileFormatCodes.GbkG1, mosalId: true), Grid(BankFileFormatCodes.GbkG2, mosalId: true) },
            ["GBK"] = new[] { Grid(BankFileFormatCodes.GbkG1, mosalId: true), Grid(BankFileFormatCodes.GbkG2, mosalId: true) },
            ["ABK"] = new[]
            {
                Grid(BankFileFormatCodes.AbkG1, payeeBankCodes: true),
                Grid(BankFileFormatCodes.AbkG2, payeeBankCodes: true),
                Grid(BankFileFormatCodes.AbkG3, payeeBankCodes: true),
            },
            ["BUR"] = new[]
            {
                Text(BankFileFormatCodes.BurT1, mosalId: true),
                Grid(BankFileFormatCodes.BurG2, companyCode: true, mosalId: true),
                Grid(BankFileFormatCodes.BurG1, employeeAccount: true, payeeBankCodes: true, mosalId: true),
            },
            ["KFH"] = new[] { Grid(BankFileFormatCodes.KfhG1) },
            ["KIB"] = new[] { Grid(BankFileFormatCodes.KibG1) },
            ["WRB"] = new[] { Grid(BankFileFormatCodes.WrbG1) },
            ["BB"] = new[] { Grid(BankFileFormatCodes.BbG1) },
            ["NBR"] = new[] { Grid(BankFileFormatCodes.NbrG1) },
            ["BURBD"] = new[]
            {
                Text(BankFileFormatCodes.BurBdT1, employeeAccount: true, mosalId: true),
                Grid(BankFileFormatCodes.BurBdG1, employeeAccount: true, mosalId: true),
            },
        };

    /// <summary>
    /// The Kuwait pack's banks by name, for a tenant seeded before the pack gave them these codes -- when every
    /// bank was "1" to "11" and the generator recognised none of them.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> KuwaitBankCodesByName =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["National Bank of Kuwait"] = "NBK",
            ["Commercial Bank of Kuwait"] = "CBK",
            ["Al-Ahli Bank of Kuwait"] = "ABK",
            ["Gulf Bank"] = "GB",
            ["Burgan Bank"] = "BUR",
            ["Kuwait Finance House"] = "KFH",
            ["Kuwait International Bank"] = "KIB",
            ["Warba Bank"] = "WRB",
            ["Boubyan Bank"] = "BB",
        };

    /// <summary>
    /// What a paying bank calls each other bank in its salary file, by bank code. Read out of the legacy
    /// databases (HRMS_RCF and HRMS_ASF agree row for row): Al-Ahli writes Kuwait's clearing numbers, and
    /// every other bank's file carries the payee bank's own code, which is the fallback below. Nobody is asked
    /// for these -- the sheet already says which bank each employee is paid into.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> PayeeBankCodesByPayer =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ABK"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["NBK"] = "001",
                ["CBK"] = "002",
                ["GB"] = "003",
                ["GBK"] = "003",
                ["ABK"] = "004",
                ["AUB"] = "005",
                ["KIB"] = "006",
                ["BUR"] = "007",
                ["BBK"] = "008",
                ["KFH"] = "009",
                ["BB"] = "013",
                ["WRB"] = "023",
            },
        };

    private const string RegularRun = "R";

    private readonly PayrollBankFiles _bankFiles;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;
    private readonly GoLiveV2PayrollResultsReader _payrollResults;
    private readonly GoLiveV2CompanySponsor _companySponsor;
    private readonly GoLiveV2GridReader _gridReader;
    private readonly IRepository<Bank> _bankRepository;
    private readonly IRepository<BankFile> _bankFileRepository;
    private readonly IRepository<OrganizationBankInfo> _organizationBankInfoRepository;
    private readonly IRepository<EmployeeBank> _employeeBankRepository;
    private readonly IRepository<PayeeBankDetail> _payeeBankDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _assignmentRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<OrganizationUnit, long> _orgUnitRepository;
    private readonly IAbpSession _session;

    public GoLiveV2BankFileStep(
        PayrollBankFiles bankFiles,
        IEmployeePayrollAreaResolver payrollAreaResolver,
        GoLiveV2PayrollResultsReader payrollResults,
        GoLiveV2CompanySponsor companySponsor,
        GoLiveV2GridReader gridReader,
        IRepository<Bank> bankRepository,
        IRepository<BankFile> bankFileRepository,
        IRepository<OrganizationBankInfo> organizationBankInfoRepository,
        IRepository<EmployeeBank> employeeBankRepository,
        IRepository<PayeeBankDetail> payeeBankDetailRepository,
        IRepository<EmployeeOrgAssignment> assignmentRepository,
        IRepository<Employee> employeeRepository,
        IRepository<OrganizationUnit, long> orgUnitRepository,
        IAbpSession session)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;

        _bankFiles = bankFiles;
        _payrollAreaResolver = payrollAreaResolver;
        _payrollResults = payrollResults;
        _companySponsor = companySponsor;
        _gridReader = gridReader;
        _bankRepository = bankRepository;
        _bankFileRepository = bankFileRepository;
        _organizationBankInfoRepository = organizationBankInfoRepository;
        _employeeBankRepository = employeeBankRepository;
        _payeeBankDetailRepository = payeeBankDetailRepository;
        _assignmentRepository = assignmentRepository;
        _employeeRepository = employeeRepository;
        _orgUnitRepository = orgUnitRepository;
        _session = session;
    }

    public async Task<GoLiveV2BankFileSetupOutput> GetSetupAsync(string payrollAreaName, DateTime monthEnd)
    {
        var output = new GoLiveV2BankFileSetupOutput();

        var banks = await _bankRepository.GetAll()
            .Include(b => b.BankFile)
            .OrderBy(b => b.NameEn)
            .ToListAsync();

        var accounts = await _organizationBankInfoRepository.GetAll()
            .Where(o => o.BankId != null && o.AccountNo != null && o.AccountNo != "")
            .Select(o => new { BankId = o.BankId.Value, o.AccountNo })
            .ToListAsync();

        foreach (var bank in banks)
        {
            var code = LayoutCodeOf(bank);
            if (code == null)
            {
                continue;
            }

            var layouts = LayoutsByBankCode[code];
            var assigned = bank.BankFile?.NameEn;

            output.PayingBanks.Add(new GoLiveV2PayingBankDto
            {
                BankId = bank.Id,
                Name = bank.NameEn,
                Code = code,
                SelectedLayout = layouts.Any(l => l.FormatCode == assigned) ? assigned : layouts[0].FormatCode,
                CompanyAccountNo = accounts.FirstOrDefault(a => a.BankId == bank.Id)?.AccountNo,
                BankCode = bank.ExternalCode,
                CompanyCode = bank.CompanyCode,
                Layouts = layouts.Select(l => l.ToDto()).ToList(),
            });
        }

        output.MosalId = await _companySponsor.CurrentMosalIdAsync();

        var employees = await EmployeesAsync(payrollAreaName, monthEnd);
        var employeeIds = employees.Select(e => e.Id).ToList();
        var details = await BankDetailsAsync(employeeIds, monthEnd);
        var banksInUse = await BanksEmployeesArePaidIntoAsync(employeeIds, monthEnd);
        var payeeCodes = await _payeeBankDetailRepository.GetAll()
            .Where(d => d.BankId != null && d.PayeeBankId != null)
            .Select(d => new { PayerBankId = d.BankId.Value, PayeeBankId = d.PayeeBankId.Value, d.BankCode })
            .ToListAsync();

        foreach (var payingBank in output.PayingBanks)
        {
            payingBank.PayeeBanks = banksInUse
                .Select(b => new GoLiveV2PayeeBankDto
                {
                    BankId = b.BankId,
                    BankName = b.BankName,
                    Employees = b.Employees,
                    Code = Given(payeeCodes
                              .FirstOrDefault(c => c.PayerBankId == payingBank.BankId && c.PayeeBankId == b.BankId)?.BankCode)
                           ?? PayeeBankCodeOf(payingBank.Code, b.BankCode),
                })
                .ToList();
        }

        output.EmployeesWithBank = employees.Count(e => details.ContainsKey(e.Id));
        output.EmployeesWithAccountNumber = employees.Count(e => details.TryGetValue(e.Id, out var d) && d);
        output.EmployeesWithoutBank = employees
            .Where(e => !details.ContainsKey(e.Id))
            .Select(e => LeftOut(e.Code, e.Name, GoLiveV2BankFileLeftOutReason.NoBankDetails))
            .ToList();

        return output;
    }

    public async Task<GoLiveV2GenerateBankFileOutput> GenerateAsync(GoLiveV2GenerateBankFileInput input, DateTime monthEnd)
    {
        var bank = await _bankRepository.FirstOrDefaultAsync(input.PaymentBankId)
                   ?? throw new UserFriendlyException(L("BankFilePaymentBankNotSelected"));

        var code = LayoutCodeOf(bank) ?? throw new UserFriendlyException(L("GoLiveV2BankFileBankHasNoLayout", bank.NameEn));

        var formatCode = input.FormatCode?.Trim();
        var layout = LayoutsByBankCode[code].FirstOrDefault(l => string.Equals(l.FormatCode, formatCode, StringComparison.OrdinalIgnoreCase))
                     ?? throw new UserFriendlyException(L("GoLiveV2BankFileLayoutNotForBank", formatCode, bank.NameEn));

        // What the layout needs beyond each employee's bank and IBAN, given now or saved before.
        var companyAccount = Given(input.CompanyAccountNo) ?? await CompanyAccountAsync(bank.Id);
        if (layout.NeedsCompanyAccount && companyAccount == null)
        {
            throw new UserFriendlyException(L("GoLiveV2BankFileNeedsCompanyAccount", layout.FormatCode));
        }

        if (layout.NeedsCompanyCode && (Given(input.CompanyCode) ?? Given(bank.CompanyCode)) == null)
        {
            throw new UserFriendlyException(L("GoLiveV2BankFileNeedsCompanyCode", layout.FormatCode));
        }

        var mosalId = input.MosalId ?? await _companySponsor.CurrentMosalIdAsync();
        if (layout.NeedsMosalId && (mosalId ?? 0) <= 0)
        {
            throw new UserFriendlyException(L("GoLiveV2BankFileNeedsMosalId", layout.FormatCode));
        }

        // Who the file pays: everyone in the area the month paid, with a bank and an IBAN. The rest are named.
        var results = await _payrollResults.GetResultsAsync(input.PayrollAreaName, monthEnd);
        var details = await BankDetailsAsync(results.Employees.Select(e => e.EmployeeId).ToList(), monthEnd);

        var output = new GoLiveV2GenerateBankFileOutput();
        var payees = new List<string>();
        var payeeIds = new List<int>();

        foreach (var employee in results.Employees)
        {
            if (!employee.HasRegister || employee.NetPay == 0)
            {
                output.LeftOut.Add(LeftOut(employee.EmployeeCode, employee.EmployeeName, GoLiveV2BankFileLeftOutReason.NotPaid));
            }
            else if (!details.TryGetValue(employee.EmployeeId, out var hasAccountNumber))
            {
                output.LeftOut.Add(LeftOut(employee.EmployeeCode, employee.EmployeeName, GoLiveV2BankFileLeftOutReason.NoBankDetails));
            }
            else if (layout.NeedsEmployeeAccountNumber && !hasAccountNumber)
            {
                // This layout pays into the account number, so an employee without one would come out blank.
                output.LeftOut.Add(LeftOut(employee.EmployeeCode, employee.EmployeeName, GoLiveV2BankFileLeftOutReason.NoAccountNumber));
            }
            else
            {
                payees.Add(employee.EmployeeCode);
                payeeIds.Add(employee.EmployeeId);
            }
        }

        if (payees.Count == 0)
        {
            throw new UserFriendlyException(L("GoLiveV2BankFileNobodyToPay", monthEnd.ToString("MMMM yyyy", CultureInfo.InvariantCulture)));
        }

        output.BankCodeCorrectedFrom = await SetUpBankAsync(bank, code, layout, input, companyAccount, input.PayrollAreaName, monthEnd);

        if (layout.WritesPayeeBankCodes)
        {
            await EnsurePayeeBankCodesAsync(bank, payeeIds, monthEnd);
        }

        int? sponsorId = null;
        if (mosalId > 0)
        {
            sponsorId = await _companySponsor.EnsureAsync(
                await CompanyNameAsync(input.PayrollAreaName, monthEnd),
                await CompanyUnitIdAsync(input.PayrollAreaName, monthEnd) ?? 0,
                mosalId.Value,
                payeeIds);
        }

        var file = await _bankFiles.GenerateBankFile(new GenerateBankFileInput
        {
            PaymentBankId = bank.Id,
            PayrollRunType = RegularRun,
            PaymentType = PaymentType.Bank,
            EmployeeCodes = string.Join(",", payees),
            // Named only where the layout writes its MOSAL ID in a header: it also narrows the file to that
            // sponsor's employees, and every employee in the file has just been put under it.
            SponsorId = layout.NeedsMosalId ? sponsorId : null,
            Year = monthEnd.Year,
            Month = monthEnd.Month,
            StartDate = new DateTime(monthEnd.Year, monthEnd.Month, 1),
            EndDate = monthEnd,
        });

        output.FileName = file.FileName;
        output.Base64Content = file.Base64Content;
        output.ContentType = file.ContentType;
        output.FormatCode = file.FormatCode;
        output.RecordCount = file.RecordCount;
        output.TotalAmount = file.TotalAmount;
        output.Contents = ReadContents(file);

        return output;
    }

    /// <summary>
    /// What the generated file holds, so the screen can show it before anyone sends it to the bank: the
    /// rows of a spreadsheet, the lines of a fixed-width file, and nothing for a zip of several.
    /// </summary>
    private GoLiveV2GridDto ReadContents(BankFileOutput file)
    {
        if (string.IsNullOrEmpty(file.Base64Content))
        {
            return null;
        }

        var bytes = Convert.FromBase64String(file.Base64Content);
        var contentType = file.ContentType ?? string.Empty;

        if (contentType.Contains("text"))
        {
            return _gridReader.ReadText(Encoding.UTF8.GetString(bytes), file.FormatCode);
        }

        return contentType.Contains("zip") ? null : _gridReader.Read(bytes, file.FormatCode);
    }

    /// <summary>
    /// Saves the choices where the Bank File screen reads them: the bank's code as the generator knows it, its
    /// layout, its external and company codes, and the company's account there. The layout rows are seeded
    /// first, as that screen's own button does. Returns the bank's old code when it had to be corrected.
    /// </summary>
    private async Task<string> SetUpBankAsync(
        Bank bank,
        string code,
        Layout layout,
        GoLiveV2GenerateBankFileInput input,
        string companyAccount,
        string payrollAreaName,
        DateTime monthEnd)
    {
        await _bankFiles.SeedBankFileFormats();
        await CurrentUnitOfWork.SaveChangesAsync();

        string correctedFrom = null;
        if (!string.Equals(bank.Code?.Trim(), code, StringComparison.OrdinalIgnoreCase))
        {
            correctedFrom = bank.Code;
            bank.Code = code;
        }

        var bankFile = await _bankFileRepository.FirstOrDefaultAsync(f => f.NameEn == layout.FormatCode);
        bank.BankFileId = bankFile?.Id;

        // What the paying bank calls ITSELF in its own file -- Al-Ahli writes "004" for an Al-Ahli
        // account, every other bank its own code. Set only when nobody has set it by hand.
        bank.ExternalCode = Given(bank.ExternalCode) ?? PayeeBankCodeOf(code, code);
        bank.CompanyCode = Given(input.CompanyCode) ?? bank.CompanyCode;

        if (Given(input.CompanyAccountNo) != null)
        {
            await SaveCompanyAccountAsync(bank.Id, companyAccount, payrollAreaName, monthEnd);
        }

        await CurrentUnitOfWork.SaveChangesAsync();
        return correctedFrom;
    }

    /// <summary>
    /// This layout writes a code beside each employee for the bank they are paid into, and the generator reads
    /// that code from the paying bank's own list -- which is why the column came out empty for a company that
    /// had never filled the list in. The codes are known, so they are written rather than asked for: the
    /// paying bank's published list where there is one, otherwise the payee bank's own code, which is what
    /// every other bank's file carries. A code somebody has set by hand is left alone.
    /// </summary>
    private async Task EnsurePayeeBankCodesAsync(Bank payerBank, List<int> payeeIds, DateTime monthEnd)
    {
        var banksInUse = await BanksEmployeesArePaidIntoAsync(payeeIds, monthEnd);
        var existing = await _payeeBankDetailRepository.GetAll()
            .Where(d => d.BankId == payerBank.Id && d.PayeeBankId != null)
            .ToListAsync();

        foreach (var payee in banksInUse)
        {
            var code = PayeeBankCodeOf(payerBank.Code, payee.BankCode);
            if (code == null)
            {
                continue;
            }

            var row = existing.FirstOrDefault(d => d.PayeeBankId == payee.BankId);
            if (row != null)
            {
                row.BankCode = Given(row.BankCode) ?? code;
                continue;
            }

            await _payeeBankDetailRepository.InsertAsync(new PayeeBankDetail
            {
                TenantId = _session.GetTenantId(),
                BankId = payerBank.Id,
                PayeeBankId = payee.BankId,
                BankCode = code,
            });
        }

        await CurrentUnitOfWork.SaveChangesAsync();
    }

    /// <summary>The paying bank's own name for a payee bank, or the payee bank's code where it has no list.</summary>
    private static string PayeeBankCodeOf(string payerBankCode, string payeeBankCode)
    {
        if (payerBankCode != null
            && PayeeBankCodesByPayer.TryGetValue(payerBankCode, out var codes)
            && payeeBankCode != null
            && codes.TryGetValue(payeeBankCode, out var code))
        {
            return code;
        }

        return Given(payeeBankCode);
    }

    /// <summary>The banks these employees are actually paid into, and how many are paid into each.</summary>
    private async Task<List<(int BankId, string BankName, string BankCode, int Employees)>> BanksEmployeesArePaidIntoAsync(
        List<int> employeeIds,
        DateTime monthEnd) =>
        (await _employeeBankRepository.GetAll()
            .Where(b => employeeIds.Contains(b.EmployeeId)
                        && b.StartDate <= monthEnd && b.EndDate >= monthEnd
                        && b.PaymentType == PaymentType.Bank
                        && b.BankId != null)
            .Select(b => new { BankId = b.BankId.Value, BankName = b.Bank.NameEn, BankCode = b.Bank.Code, b.EmployeeId })
            .ToListAsync())
        .GroupBy(b => new { b.BankId, b.BankName, b.BankCode })
        .OrderBy(g => g.Key.BankName)
        .Select(g => (g.Key.BankId, g.Key.BankName, g.Key.BankCode, g.Select(b => b.EmployeeId).Distinct().Count()))
        .ToList();

    /// <summary>The company the payroll area's employees are in, by the name its organisation unit carries.</summary>
    private async Task<string> CompanyNameAsync(string payrollAreaName, DateTime monthEnd)
    {
        var unitId = await CompanyUnitIdAsync(payrollAreaName, monthEnd);
        return unitId == null ? null : (await _orgUnitRepository.FirstOrDefaultAsync(unitId.Value))?.DisplayName;
    }

    /// <summary>The company's account at the paying bank: the one row per bank, on the area's company unit.</summary>
    private async Task SaveCompanyAccountAsync(int bankId, string accountNo, string payrollAreaName, DateTime monthEnd)
    {
        var existing = await _organizationBankInfoRepository.FirstOrDefaultAsync(o => o.BankId == bankId);
        if (existing != null)
        {
            existing.AccountNo = accountNo;
            return;
        }

        await _organizationBankInfoRepository.InsertAsync(new OrganizationBankInfo
        {
            TenantId = _session.GetTenantId(),
            BankId = bankId,
            AccountNo = accountNo,
            OrganizationUnitId = await CompanyUnitIdAsync(payrollAreaName, monthEnd),
        });
    }

    /// <summary>The organisation unit most of the area's employees are assigned to -- for a V2 load, the company.</summary>
    private async Task<long?> CompanyUnitIdAsync(string payrollAreaName, DateTime monthEnd)
    {
        var ids = await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(payrollAreaName.Trim(), monthEnd);

        return (await _assignmentRepository.GetAll()
                .Where(a => ids.Contains(a.EmployeeId) && a.OrganizationUnitId != null)
                .Select(a => a.OrganizationUnitId.Value)
                .ToListAsync())
            .GroupBy(id => id)
            .OrderByDescending(g => g.Count())
            .Select(g => (long?)g.Key)
            .FirstOrDefault();
    }

    private async Task<string> CompanyAccountAsync(int bankId) =>
        Given((await _organizationBankInfoRepository.FirstOrDefaultAsync(o => o.BankId == bankId && o.AccountNo != null && o.AccountNo != ""))?.AccountNo);

    private async Task<List<(int Id, string Code, string Name)>> EmployeesAsync(string payrollAreaName, DateTime monthEnd)
    {
        var ids = await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(payrollAreaName.Trim(), monthEnd);

        return (await _employeeRepository.GetAll()
                .Where(e => ids.Contains(e.Id))
                .OrderBy(e => e.Code)
                .Select(e => new { e.Id, e.Code, e.FullNameEn })
                .ToListAsync())
            .Select(e => (e.Id, e.Code, e.FullNameEn))
            .ToList();
    }

    /// <summary>
    /// Everyone with a bank-transfer record in force at month end, with a bank and an IBAN -- what a bank file
    /// pays into -- and whether that record also carries an account number, which some layouts pay into instead.
    /// </summary>
    private async Task<Dictionary<int, bool>> BankDetailsAsync(List<int> employeeIds, DateTime monthEnd) =>
        (await _employeeBankRepository.GetAll()
            .Where(b => employeeIds.Contains(b.EmployeeId)
                        && b.StartDate <= monthEnd && b.EndDate >= monthEnd
                        && b.PaymentType == PaymentType.Bank
                        && b.BankId != null
                        && b.IBAN != null && b.IBAN != "")
            .Select(b => new { b.EmployeeId, HasAccountNumber = b.AccountNum != null && b.AccountNum != "" })
            .ToListAsync())
        .GroupBy(b => b.EmployeeId)
        .ToDictionary(g => g.Key, g => g.Any(b => b.HasAccountNumber));

    /// <summary>
    /// The code the generator knows the bank by: its own code when that has layouts, otherwise the Kuwait pack's
    /// code for its name. Null for a bank no layout was ever written for.
    /// </summary>
    private static string LayoutCodeOf(Bank bank)
    {
        var own = bank.Code?.Trim();
        if (!string.IsNullOrEmpty(own) && LayoutsByBankCode.ContainsKey(own))
        {
            return own.ToUpperInvariant();
        }

        return bank.NameEn != null && KuwaitBankCodesByName.TryGetValue(bank.NameEn.Trim(), out var byName) ? byName : null;
    }

    private static string Given(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static GoLiveV2BankFileEmployeeDto LeftOut(string code, string name, GoLiveV2BankFileLeftOutReason reason) =>
        new() { EmployeeCode = code, EmployeeName = name, Reason = reason };

    private static Layout Grid(
        string formatCode,
        bool companyAccount = false,
        bool companyCode = false,
        bool employeeAccount = false,
        bool payeeBankCodes = false,
        bool mosalId = false) =>
        new(formatCode, false, companyAccount, companyCode, employeeAccount, payeeBankCodes, mosalId);

    private static Layout Text(
        string formatCode,
        bool companyAccount = false,
        bool companyCode = false,
        bool employeeAccount = false,
        bool payeeBankCodes = false,
        bool mosalId = false) =>
        new(formatCode, true, companyAccount, companyCode, employeeAccount, payeeBankCodes, mosalId);

    private sealed record Layout(
        string FormatCode,
        bool IsText,
        bool NeedsCompanyAccount,
        bool NeedsCompanyCode,
        bool NeedsEmployeeAccountNumber,
        bool WritesPayeeBankCodes,
        bool NeedsMosalId)
    {
        public GoLiveV2BankFileLayoutDto ToDto() => new()
        {
            FormatCode = FormatCode,
            IsText = IsText,
            NeedsCompanyAccount = NeedsCompanyAccount,
            NeedsCompanyCode = NeedsCompanyCode,
            NeedsEmployeeAccountNumber = NeedsEmployeeAccountNumber,
            WritesPayeeBankCodes = WritesPayeeBankCodes,
            NeedsMosalId = NeedsMosalId,
        };
    }
}
