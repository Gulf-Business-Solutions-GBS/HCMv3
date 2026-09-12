using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Runtime.Validation;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GoLiveSetupV2.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GoLiveSetupV2;

/// <summary>
/// Turns "Load Employee Data" grid rows into employees, each with an open-ended contract, org assignment and
/// monthly salary from the joining date, the payroll status both engines keep per employee, and the working
/// time -- duty period and time policy -- the time engine works each day out from. Every record
/// goes through its own service, so the checks those screens make -- duplicate code, duplicate civil ID among
/// active staff, overlapping contracts -- apply here too.
///
/// <para><b>What is not asked, and what it is set to.</b> The employee record needs more than the grid asks.
/// Nothing is guessed: every lookup points at its NO_ record (NO_NATIONALITY, NO_TITLE, ...), gender is "not
/// specified", the Arabic name is the English one, and the date of birth is 1 January 1900 -- chosen because
/// nobody could mistake it for a real one. All of it can be corrected on the employee screen later.</para>
/// </summary>
public class GoLiveV2EmployeeLoader : AbpServiceBase, ITransientDependency
{
    public static readonly DateTime PlaceholderBirthDate = new(1900, 1, 1);

    /// <summary>
    /// The grid's salary columns and the wage type each is paid as, by the payroll catalogue's codes, in the
    /// customer's order: basic, accommodation, mobile, transport, then the rest. In the Default tenant's live
    /// data basic salary is on every employee, and accommodation, food and transport are the commonest
    /// allowances.
    /// </summary>
    public static readonly (string WageCode, Func<GoLiveV2EmployeeRowDto, double?> Amount)[] SalaryColumns =
    {
        ("1000", r => r.BasicSalary),
        ("1027", r => r.AccommodationAllowance),
        ("1011", r => r.MobileAllowance),
        ("1002", r => r.TransportAllowance),
        ("1038", r => r.FoodAllowance),
        ("1001", r => r.HousingAllowance),
        ("1018", r => r.OtherAllowance),
    };

    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    /// <summary>Two letters, two check digits, then 10 to 30 letters and digits -- the shape every country's IBAN has.</summary>
    private static readonly Regex IbanShape = new("^[A-Z]{2}[0-9]{2}[A-Z0-9]{10,30}$", RegexOptions.Compiled);

    /// <summary>A Kuwaiti IBAN is always this long: KW, two check digits, a four-letter bank code and 22 more.</summary>
    private const int KuwaitIbanLength = 30;

    private readonly IEmployeesAppService _employees;
    private readonly IEmployeeContractsAppService _contracts;
    private readonly IEmployeeOrgAssignmentsAppService _assignments;
    private readonly IEmployeeBasicPaysAppService _basicPays;
    private readonly IEmployeeBasicPayDetailsAppService _basicPayDetails;
    private readonly IEmployeePayrollStatusesAppService _payrollStatuses;
    private readonly IEmployeeBanksAppService _employeeBanks;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeContract> _contractRepository;
    private readonly IRepository<EmployeeOrgAssignment> _assignmentRepository;
    private readonly IRepository<EmployeeBasicPay> _basicPayRepository;
    private readonly IRepository<EmployeePayrollStatus> _payrollStatusRepository;
    private readonly IRepository<EmployeeBank> _employeeBankRepository;
    private readonly GoLiveV2WorkingTime _workingTime;

    public GoLiveV2EmployeeLoader(
        IEmployeesAppService employees,
        IEmployeeContractsAppService contracts,
        IEmployeeOrgAssignmentsAppService assignments,
        IEmployeeBasicPaysAppService basicPays,
        IEmployeeBasicPayDetailsAppService basicPayDetails,
        IEmployeePayrollStatusesAppService payrollStatuses,
        IEmployeeBanksAppService employeeBanks,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeContract> contractRepository,
        IRepository<EmployeeOrgAssignment> assignmentRepository,
        IRepository<EmployeeBasicPay> basicPayRepository,
        IRepository<EmployeePayrollStatus> payrollStatusRepository,
        IRepository<EmployeeBank> employeeBankRepository,
        GoLiveV2WorkingTime workingTime)
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;

        _employees = employees;
        _contracts = contracts;
        _assignments = assignments;
        _basicPays = basicPays;
        _basicPayDetails = basicPayDetails;
        _payrollStatuses = payrollStatuses;
        _employeeBanks = employeeBanks;
        _employeeRepository = employeeRepository;
        _contractRepository = contractRepository;
        _assignmentRepository = assignmentRepository;
        _basicPayRepository = basicPayRepository;
        _payrollStatusRepository = payrollStatusRepository;
        _employeeBankRepository = employeeBankRepository;
        _workingTime = workingTime;
    }

    public async Task LoadAsync(IReadOnlyList<GoLiveV2EmployeeRowDto> rows, GoLiveV2EmployeeTargets targets, GoLiveV2LoadEmployeesOutput output)
    {
        // Repeats are caught here rather than by the services: the second row would otherwise be reported as
        // clashing with the employee this same load had just created, which reads as a database problem.
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenCivilIds = new HashSet<long>();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (IsEmpty(row))
            {
                continue;
            }

            // Numbered by position, blanks included, so the number is the one the grid shows beside the row.
            var code = row.Code?.Trim();
            var result = new GoLiveV2EmployeeRowResultDto { RowNumber = i + 1, Code = code };
            output.Rows.Add(result);

            try
            {
                var (civilId, joined) = Check(row, code);
                var salaryBank = CheckBank(row, targets.BankIdsByName);

                if (!seenCodes.Add(code))
                {
                    throw new GoLiveV2RowException(L("GoLiveV2RowCodeRepeated", code));
                }

                if (!seenCivilIds.Add(civilId))
                {
                    throw new GoLiveV2RowException(L("GoLiveV2RowCivilIdRepeated", civilId));
                }

                var employee = await _employeeRepository.FirstOrDefaultAsync(e => e.Code == code);
                if (employee == null)
                {
                    employee = await CreateEmployeeAsync(code, row.FullName.Trim(), joined, civilId, targets.Placeholders);
                    result.Created = true;
                    output.EmployeesCreated++;
                }
                else
                {
                    // Left as it is. Only a missing contract, assignment or salary is filled in, which is how a
                    // load that failed half-way through a row -- employee made, contract refused -- is finished.
                    output.EmployeesAlreadyLoaded++;
                }

                await EnsureContractAsync(employee, targets.ContractTypeId);
                await EnsureOrgAssignmentAsync(employee, targets);
                await EnsureSalaryAsync(employee, row, targets);
                await EnsurePayrollStatusAsync(employee);
                await _workingTime.EnsureEmployeeAsync(employee, targets.WorkingTime);
                await EnsureBankAsync(employee, salaryBank);

                result.Succeeded = true;
            }
            catch (GoLiveV2RowException ex)
            {
                result.Message = ex.Message;
            }
            catch (UserFriendlyException ex)
            {
                // The services' own checks. Their messages are already written for the customer.
                result.Message = ex.Message;
            }
            catch (AbpValidationException ex)
            {
                result.Message = string.Join(" ", ex.ValidationErrors.Select(e => e.ErrorMessage));
            }
        }
    }

    /// <summary>The four identity columns and basic salary are required. Returns the two that are not plain text.</summary>
    private (long CivilId, DateTime Joined) Check(GoLiveV2EmployeeRowDto row, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new GoLiveV2RowException(L("GoLiveV2RowCodeMissing"));
        }

        if (string.IsNullOrWhiteSpace(row.FullName))
        {
            throw new GoLiveV2RowException(L("GoLiveV2RowNameMissing"));
        }

        // The employee record needs a first and a last name; one word cannot be split into both.
        if (row.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2)
        {
            throw new GoLiveV2RowException(L("GoLiveV2RowNameOneWord"));
        }

        if (row.JoiningDate == null)
        {
            throw new GoLiveV2RowException(L("GoLiveV2RowJoiningDateMissing"));
        }

        var civilId = row.CivilId?.Trim();
        if (string.IsNullOrEmpty(civilId))
        {
            throw new GoLiveV2RowException(L("GoLiveV2RowCivilIdMissing"));
        }

        if (!civilId.All(char.IsDigit) || !long.TryParse(civilId, out var parsed) || parsed == 0)
        {
            throw new GoLiveV2RowException(L("GoLiveV2RowCivilIdNotANumber", civilId));
        }

        // Without a basic salary there is nothing for the payroll to pay; the allowances are optional.
        if (row.BasicSalary is not > 0)
        {
            throw new GoLiveV2RowException(L("GoLiveV2RowBasicSalaryMissing"));
        }

        if (SalaryColumns.Any(c => c.Amount(row) < 0))
        {
            throw new GoLiveV2RowException(L("GoLiveV2RowAmountNegative"));
        }

        // The date part only: the screen sends midday so a time zone cannot move it to the day before.
        return (parsed, row.JoiningDate.Value.Date);
    }

    /// <summary>
    /// The bank and IBAN go together: both, or neither. The bank must be one the tenant has, by name -- the grid
    /// offers them as a list -- and the IBAN must have an IBAN's shape once spaces are dropped and letters
    /// capitalised, which is how it is kept. Null when neither is given: the employee is paid, not by bank file.
    /// </summary>
    private SalaryBank CheckBank(GoLiveV2EmployeeRowDto row, IReadOnlyDictionary<string, int> bankIdsByName)
    {
        var bankName = row.BankName?.Trim();
        var iban = string.IsNullOrWhiteSpace(row.Iban) ? null : row.Iban.Trim();
        var accountNumber = string.IsNullOrWhiteSpace(row.AccountNumber) ? null : row.AccountNumber.Trim();

        if (string.IsNullOrEmpty(bankName) && iban == null && accountNumber == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(bankName))
        {
            throw new GoLiveV2RowException(L("GoLiveV2RowBankMissing"));
        }

        if (!bankIdsByName.TryGetValue(bankName, out var bankId))
        {
            throw new GoLiveV2RowException(L("GoLiveV2RowBankUnknown", bankName));
        }

        if (iban == null)
        {
            throw new GoLiveV2RowException(L("GoLiveV2RowIbanMissing"));
        }

        var normalized = new string(iban.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        if (!IbanShape.IsMatch(normalized) || (normalized.StartsWith("KW") && normalized.Length != KuwaitIbanLength))
        {
            throw new GoLiveV2RowException(L("GoLiveV2RowIbanInvalid", iban));
        }

        return new SalaryBank(bankId, normalized, accountNumber);
    }

    private async Task<Employee> CreateEmployeeAsync(
        string code,
        string fullName,
        DateTime joined,
        long civilId,
        GoLiveV2EmployeePlaceholders placeholders)
    {
        var (first, last) = SplitName(fullName);

        await _employees.CreateOrEdit(new CreateOrEditEmployeeDto
        {
            Code = code,
            FirstNameEn = first,
            LastNameEn = last,
            FirstNameAr = first,
            LastNameAr = last,
            JoiningDate = joined,
            BirthDate = PlaceholderBirthDate,
            Gender = Gender.NotSpecified,
            CivilID = civilId,
            NationalityId = placeholders.NationalityId,
            TitleId = placeholders.TitleId,
            MaritalStatusId = placeholders.MaritalStatusId,
            ReligionId = placeholders.ReligionId,
            GovernorateId = placeholders.GovernorateId,
            CountryId = placeholders.CountryId,
            QualificationId = placeholders.QualificationId,
            BirthPlaceId = placeholders.BirthPlaceId,
            RecruitmentTypeId = placeholders.RecruitmentTypeId,
        });
        await CurrentUnitOfWork.SaveChangesAsync();

        return await _employeeRepository.FirstOrDefaultAsync(e => e.Code == code);
    }

    private async Task EnsureContractAsync(Employee employee, int contractTypeId)
    {
        if (await _contractRepository.GetAll().AnyAsync(c => c.EmployeeId == employee.Id))
        {
            return;
        }

        await _contracts.CreateOrEdit(new CreateOrEditEmployeeContractDto
        {
            EmployeeId = employee.Id,
            ContractTypeId = contractTypeId,
            StartDate = employee.JoiningDate,
            EndDate = OpenEnded,
        });
        await CurrentUnitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// The assignment service's form has no payroll area, group or sub-group yet -- they were added to the
    /// entity after the screen was built -- so they are set on the saved row straight after, as V1 does.
    /// </summary>
    private async Task EnsureOrgAssignmentAsync(Employee employee, GoLiveV2EmployeeTargets targets)
    {
        if (await _assignmentRepository.GetAll().AnyAsync(a => a.EmployeeId == employee.Id))
        {
            return;
        }

        await _assignments.CreateOrEdit(new CreateOrEditEmployeeOrgAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = employee.JoiningDate,
            OrganizationUnitId = targets.OrganizationUnitId,
        });
        await CurrentUnitOfWork.SaveChangesAsync();

        var saved = await _assignmentRepository.FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.StartDate == employee.JoiningDate);
        saved.EndDate = OpenEnded;
        saved.PayrollAreaId = targets.PayrollAreaId;
        saved.EmployeeGroupId = targets.EmployeeGroupId;
        saved.EmployeeSubGroupId = targets.EmployeeSubGroupId;
        await CurrentUnitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// One monthly pay record from the joining date, open-ended, with a line per non-empty amount. An employee
    /// who already has a pay record keeps it: salary history is the customer's, not the loader's to rewrite.
    /// </summary>
    private async Task EnsureSalaryAsync(Employee employee, GoLiveV2EmployeeRowDto row, GoLiveV2EmployeeTargets targets)
    {
        if (await _basicPayRepository.GetAll().AnyAsync(p => p.EmployeeId == employee.Id))
        {
            return;
        }

        await _basicPays.CreateOrEdit(new CreateOrEditEmployeeBasicPayDto
        {
            EmployeeId = employee.Id,
            StartDate = employee.JoiningDate,
            EndDate = OpenEnded,
            PayType = PayType.Regular,
            PayInterval = PayInterval.Monthly,
        });
        await CurrentUnitOfWork.SaveChangesAsync();

        var pay = await _basicPayRepository.FirstOrDefaultAsync(p => p.EmployeeId == employee.Id);
        double total = 0;

        foreach (var (wageCode, amountOf) in SalaryColumns)
        {
            var amount = amountOf(row) ?? 0;
            if (amount <= 0)
            {
                continue;
            }

            await _basicPayDetails.CreateOrEdit(new CreateOrEditEmployeeBasicPayDetailDto
            {
                EmployeeBasicPayId = pay.Id,
                WageTypeId = targets.SalaryWageTypeIds[wageCode],
                Amount = amount,
                CurrencyId = targets.CurrencyId,
            });
            total += amount;
        }

        // The total is what the payroll sheet and provision reports show as the salary. Neither service keeps it.
        pay.Amount = total;
        await CurrentUnitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// The per-employee record both engines keep their place in. The payroll engine refuses an employee without
    /// one; the time engine makes one on first sight, and these are its values: evaluate time from the joining
    /// date, retro accounting from the joining date, no master-data change pending, nothing accounted yet. The
    /// dates left unset stay at 0001-01-01, which both engines read as "not set" -- a DoNoAccountAfter in the
    /// past, say, would stop the employee being paid at all.
    /// </summary>
    private async Task EnsurePayrollStatusAsync(Employee employee)
    {
        if (await _payrollStatusRepository.GetAll().AnyAsync(s => s.EmployeeId == employee.Id))
        {
            return;
        }

        await _payrollStatuses.CreateOrEdit(new CreateOrEditEmployeePayrollStatusDto
        {
            EmployeeId = employee.Id,
            NextTimeEvaluationFrom = employee.JoiningDate,
            EarliestRetroAccountingDate = employee.JoiningDate,
        });
        await CurrentUnitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// The one bank record a V2 load keeps per employee: paid by bank transfer from the joining date, open-ended,
    /// with no paying bank of its own so whichever bank the company pays from covers it. Loading the sheet again
    /// with a different bank or IBAN corrects that record rather than adding a second -- the grid is where a
    /// mistyped IBAN gets fixed. A row with no bank leaves any bank record there as it is.
    /// </summary>
    private async Task EnsureBankAsync(Employee employee, SalaryBank salaryBank)
    {
        if (salaryBank == null)
        {
            return;
        }

        var (bankId, iban, accountNumber) = salaryBank;
        var existing = await _employeeBankRepository.GetAll()
            .Where(b => b.EmployeeId == employee.Id)
            .OrderByDescending(b => b.StartDate)
            .FirstOrDefaultAsync();

        // An account number the sheet leaves blank is kept: it may have been filled in on the employee screen.
        accountNumber ??= existing?.AccountNum;

        if (existing != null && existing.BankId == bankId && existing.IBAN == iban
            && existing.AccountNum == accountNumber && existing.PaymentType == PaymentType.Bank)
        {
            return;
        }

        await _employeeBanks.CreateOrEdit(new CreateOrEditEmployeeBankDto
        {
            Id = existing?.Id,
            EmployeeId = employee.Id,
            BankId = bankId,
            PaymentType = PaymentType.Bank,
            IBAN = iban,
            AccountNum = accountNumber,
            BankBranch = existing?.BankBranch,
            BankCountryId = existing?.BankCountryId,
            PaymentCurrencyId = existing?.PaymentCurrencyId,
            PayerBankId = existing?.PayerBankId,
            StartDate = existing?.StartDate ?? employee.JoiningDate,
            EndDate = existing?.EndDate ?? OpenEnded,
        });
        await CurrentUnitOfWork.SaveChangesAsync();
    }

    /// <summary>First word to the first name, the rest to the last -- which joins back to the same full name.</summary>
    private static (string First, string Last) SplitName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return (parts[0], string.Join(' ', parts.Skip(1)));
    }

    private static bool IsEmpty(GoLiveV2EmployeeRowDto row) =>
        string.IsNullOrWhiteSpace(row.Code)
        && string.IsNullOrWhiteSpace(row.FullName)
        && row.JoiningDate == null
        && string.IsNullOrWhiteSpace(row.CivilId)
        && SalaryColumns.All(c => c.Amount(row) == null)
        && string.IsNullOrWhiteSpace(row.BankName)
        && string.IsNullOrWhiteSpace(row.Iban)
        && string.IsNullOrWhiteSpace(row.AccountNumber);
}

/// <summary>Where one employee's salary is paid: the bank, the IBAN, and the account number if it was given.</summary>
public record SalaryBank(int BankId, string Iban, string AccountNumber);

/// <summary>Where every loaded employee goes: the records <see cref="GoLiveV2EmployeeLoader"/> attaches them to.</summary>
public class GoLiveV2EmployeeTargets
{
    public long OrganizationUnitId { get; set; }

    public int PayrollAreaId { get; set; }

    public int EmployeeGroupId { get; set; }

    public int EmployeeSubGroupId { get; set; }

    public int ContractTypeId { get; set; }

    public int CurrencyId { get; set; }

    /// <summary>Wage type id by catalogue code, one for each of <see cref="GoLiveV2EmployeeLoader.SalaryColumns"/>.</summary>
    public IReadOnlyDictionary<string, int> SalaryWageTypeIds { get; set; }

    public GoLiveV2EmployeePlaceholders Placeholders { get; set; }

    /// <summary>The duty period and time policy every employee is given from the joining date.</summary>
    public GoLiveV2WorkingTimeIds WorkingTime { get; set; }

    /// <summary>The tenant's banks by name, matched ignoring case -- the grid's Bank column names one of these.</summary>
    public IReadOnlyDictionary<string, int> BankIdsByName { get; set; } = new Dictionary<string, int>();
}

/// <summary>A row the grid should not have let through. Caught by the loader and reported against the row.</summary>
public class GoLiveV2RowException : Exception
{
    public GoLiveV2RowException(string message) : base(message)
    {
    }
}
