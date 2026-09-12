using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
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
/// <para><b>Which layouts are offered.</b> Only those that work from what V2 records per employee: a bank and
/// an IBAN. Layouts keyed off the employee's own account number (CBK-T1, CBK-T2, BUR-G1, the Burgan B-Dinar
/// pair, NBK-G2 for NBK customers), off a sponsor's bank account (CBK-T7), or hard-coded for one client
/// (GBK-T1) would come out with empty lines, so they are left to the Bank File screen.</para>
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
            ["NBK"] = new[] { Grid(BankFileFormatCodes.NbkG1), Grid(BankFileFormatCodes.NbkG3) },
            ["CBK"] = new[]
            {
                Grid(BankFileFormatCodes.CbkG1),
                Grid(BankFileFormatCodes.CbkG2),
                Text(BankFileFormatCodes.CbkT3, companyAccount: true, bankCode: true),
                Text(BankFileFormatCodes.CbkT4, companyAccount: true),
                Text(BankFileFormatCodes.CbkT5, companyAccount: true, bankCode: true),
                Text(BankFileFormatCodes.CbkT6, companyAccount: true, bankCode: true),
            },
            ["GB"] = new[] { Grid(BankFileFormatCodes.GbkG1), Grid(BankFileFormatCodes.GbkG2) },
            ["GBK"] = new[] { Grid(BankFileFormatCodes.GbkG1), Grid(BankFileFormatCodes.GbkG2) },
            ["ABK"] = new[]
            {
                Grid(BankFileFormatCodes.AbkG1, bankCode: true),
                Grid(BankFileFormatCodes.AbkG2, bankCode: true),
                Grid(BankFileFormatCodes.AbkG3, bankCode: true),
            },
            ["BUR"] = new[]
            {
                Text(BankFileFormatCodes.BurT1, bankCode: true),
                Grid(BankFileFormatCodes.BurG2, bankCode: true, companyCode: true),
            },
            ["KFH"] = new[] { Grid(BankFileFormatCodes.KfhG1) },
            ["KIB"] = new[] { Grid(BankFileFormatCodes.KibG1) },
            ["WRB"] = new[] { Grid(BankFileFormatCodes.WrbG1) },
            ["BB"] = new[] { Grid(BankFileFormatCodes.BbG1) },
            ["NBR"] = new[] { Grid(BankFileFormatCodes.NbrG1) },
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

    private const string RegularRun = "R";

    private readonly PayrollBankFiles _bankFiles;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;
    private readonly GoLiveV2PayrollResultsReader _payrollResults;
    private readonly IRepository<Bank> _bankRepository;
    private readonly IRepository<BankFile> _bankFileRepository;
    private readonly IRepository<OrganizationBankInfo> _organizationBankInfoRepository;
    private readonly IRepository<EmployeeBank> _employeeBankRepository;
    private readonly IRepository<EmployeeOrgAssignment> _assignmentRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IAbpSession _session;

    public GoLiveV2BankFileStep(
        PayrollBankFiles bankFiles,
        IEmployeePayrollAreaResolver payrollAreaResolver,
        GoLiveV2PayrollResultsReader payrollResults,
        IRepository<Bank> bankRepository,
        IRepository<BankFile> bankFileRepository,
        IRepository<OrganizationBankInfo> organizationBankInfoRepository,
        IRepository<EmployeeBank> employeeBankRepository,
        IRepository<EmployeeOrgAssignment> assignmentRepository,
        IRepository<Employee> employeeRepository,
        IAbpSession session)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;

        _bankFiles = bankFiles;
        _payrollAreaResolver = payrollAreaResolver;
        _payrollResults = payrollResults;
        _bankRepository = bankRepository;
        _bankFileRepository = bankFileRepository;
        _organizationBankInfoRepository = organizationBankInfoRepository;
        _employeeBankRepository = employeeBankRepository;
        _assignmentRepository = assignmentRepository;
        _employeeRepository = employeeRepository;
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

        var employees = await EmployeesAsync(payrollAreaName, monthEnd);
        var withBank = await EmployeeIdsWithBankDetailsAsync(employees.Select(e => e.Id).ToList(), monthEnd);

        output.EmployeesWithBank = employees.Count(e => withBank.Contains(e.Id));
        output.EmployeesWithoutBank = employees
            .Where(e => !withBank.Contains(e.Id))
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

        if (layout.NeedsBankCode && (Given(input.BankCode) ?? Given(bank.ExternalCode)) == null)
        {
            throw new UserFriendlyException(L("GoLiveV2BankFileNeedsBankCode", layout.FormatCode));
        }

        if (layout.NeedsCompanyCode && (Given(input.CompanyCode) ?? Given(bank.CompanyCode)) == null)
        {
            throw new UserFriendlyException(L("GoLiveV2BankFileNeedsCompanyCode", layout.FormatCode));
        }

        // Who the file pays: everyone in the area the month paid, with a bank and an IBAN. The rest are named.
        var results = await _payrollResults.GetResultsAsync(input.PayrollAreaName, monthEnd);
        var withBank = await EmployeeIdsWithBankDetailsAsync(results.Employees.Select(e => e.EmployeeId).ToList(), monthEnd);

        var output = new GoLiveV2GenerateBankFileOutput();
        var payees = new List<string>();

        foreach (var employee in results.Employees)
        {
            if (!employee.HasRegister || employee.NetPay == 0)
            {
                output.LeftOut.Add(LeftOut(employee.EmployeeCode, employee.EmployeeName, GoLiveV2BankFileLeftOutReason.NotPaid));
            }
            else if (!withBank.Contains(employee.EmployeeId))
            {
                output.LeftOut.Add(LeftOut(employee.EmployeeCode, employee.EmployeeName, GoLiveV2BankFileLeftOutReason.NoBankDetails));
            }
            else
            {
                payees.Add(employee.EmployeeCode);
            }
        }

        if (payees.Count == 0)
        {
            throw new UserFriendlyException(L("GoLiveV2BankFileNobodyToPay", monthEnd.ToString("MMMM yyyy", CultureInfo.InvariantCulture)));
        }

        output.BankCodeCorrectedFrom = await SetUpBankAsync(bank, code, layout, input, companyAccount, input.PayrollAreaName, monthEnd);

        var file = await _bankFiles.GenerateBankFile(new GenerateBankFileInput
        {
            PaymentBankId = bank.Id,
            PayrollRunType = RegularRun,
            PaymentType = PaymentType.Bank,
            EmployeeCodes = string.Join(",", payees),
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

        return output;
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

        bank.ExternalCode = Given(input.BankCode) ?? bank.ExternalCode;
        bank.CompanyCode = Given(input.CompanyCode) ?? bank.CompanyCode;

        if (Given(input.CompanyAccountNo) != null)
        {
            await SaveCompanyAccountAsync(bank.Id, companyAccount, payrollAreaName, monthEnd);
        }

        await CurrentUnitOfWork.SaveChangesAsync();
        return correctedFrom;
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

    /// <summary>A bank-transfer record in force at month end, with a bank and an IBAN -- what a bank file pays into.</summary>
    private async Task<HashSet<int>> EmployeeIdsWithBankDetailsAsync(List<int> employeeIds, DateTime monthEnd) =>
        (await _employeeBankRepository.GetAll()
            .Where(b => employeeIds.Contains(b.EmployeeId)
                        && b.StartDate <= monthEnd && b.EndDate >= monthEnd
                        && b.PaymentType == PaymentType.Bank
                        && b.BankId != null
                        && b.IBAN != null && b.IBAN != "")
            .Select(b => b.EmployeeId)
            .ToListAsync())
        .ToHashSet();

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

    private static Layout Grid(string formatCode, bool companyAccount = false, bool bankCode = false, bool companyCode = false) =>
        new(formatCode, false, companyAccount, bankCode, companyCode);

    private static Layout Text(string formatCode, bool companyAccount = false, bool bankCode = false, bool companyCode = false) =>
        new(formatCode, true, companyAccount, bankCode, companyCode);

    private sealed record Layout(string FormatCode, bool IsText, bool NeedsCompanyAccount, bool NeedsBankCode, bool NeedsCompanyCode)
    {
        public GoLiveV2BankFileLayoutDto ToDto() => new()
        {
            FormatCode = FormatCode,
            IsText = IsText,
            NeedsCompanyAccount = NeedsCompanyAccount,
            NeedsBankCode = NeedsBankCode,
            NeedsCompanyCode = NeedsCompanyCode,
        };
    }
}
