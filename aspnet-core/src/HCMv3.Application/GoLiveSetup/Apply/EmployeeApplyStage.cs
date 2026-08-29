using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.GoLiveSetup.Apply;

/// <summary>
/// Provisions the employees from step S3.1.
///
/// <para><b>Goes through IEmployeesAppService, not the repository.</b> That service owns the rules --
/// duplicate code, duplicate civil ID among active staff, joining date after birth date -- and the
/// computed FullName and ProbationEndDate. Writing rows straight through a repository would silently
/// skip all of it, which is exactly the trap the bulk import engine falls into.</para>
///
/// <para><b>No auto-generated codes.</b> The sheet requires an employee code, so the app service's
/// code generator -- which scans every existing code and is not safe under concurrency -- never runs.
/// That is deliberate, not incidental.</para>
/// </summary>
public class EmployeeApplyStage : ITransientDependency
{
    public const string StepId = "S3.1";

    /// <summary>Enough failures to diagnose the sheet without returning megabytes of them.</summary>
    private const int MaxReportedIssues = 50;

    private readonly GoLiveDraftReader _reader;
    private readonly IEmployeesAppService _employees;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<Nationality> _nationalities;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public EmployeeApplyStage(
        GoLiveDraftReader reader,
        IEmployeesAppService employees,
        IRepository<Employee> employeeRepository,
        IRepository<Nationality> nationalities,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _reader = reader;
        _employees = employees;
        _employeeRepository = employeeRepository;
        _nationalities = nationalities;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<GoLiveApplyStageDto> RunAsync(bool apply)
    {
        var stage = new GoLiveApplyStageDto { StepId = StepId, Target = nameof(Employee) };

        var rows = await _reader.ReadRowsAsync(StepId);
        if (rows.Count == 0)
        {
            return stage;
        }

        var nationalities = (await _nationalities.GetAllListAsync())
            .Where(n => !string.IsNullOrWhiteSpace(n.NameEn))
            .GroupBy(n => n.NameEn.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var existing = (await _employeeRepository.GetAllListAsync())
            .Where(e => !string.IsNullOrWhiteSpace(e.Code))
            .GroupBy(e => e.Code.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (GoLiveDraftReader.IsEmpty(row))
            {
                continue;
            }

            var rowNumber = i + 1;
            var code = GoLiveDraftReader.Text(row, "code");

            if (code == null)
            {
                Fail(stage, rowNumber, null, "Employee code is missing.");
                continue;
            }

            // Caught here rather than by the app service, because the second row would otherwise be
            // reported as clashing with the first one this same run had just created.
            if (!seenCodes.Add(code))
            {
                Fail(stage, rowNumber, code, $"Employee code '{code}' appears more than once in the sheet.");
                continue;
            }

            try
            {
                var dto = BuildDto(row, code, nationalities, existing);
                var isNew = !dto.Id.HasValue;

                if (apply)
                {
                    await _employees.CreateOrEdit(dto);

                    // EmployeesAppService forces Status = Active on create and honours the DTO status
                    // only on edit. A go-live import legitimately carries leavers -- the sheet asks
                    // "still working with you?" precisely so history can be brought across -- and
                    // leaving one Active would put them in the first payroll. So for that case only,
                    // re-issue the edit against the row the create just produced.
                    if (isNew && dto.Status != EmployeeStatus.Active)
                    {
                        // The insert is still only tracked; a LINQ query would not see it yet.
                        await _unitOfWorkManager.Current.SaveChangesAsync();

                        var created = await _employeeRepository.FirstOrDefaultAsync(e => e.Code == code);
                        if (created != null)
                        {
                            dto.Id = created.Id;
                            await _employees.CreateOrEdit(dto);
                        }
                    }
                }

                if (isNew)
                {
                    stage.Created++;
                }
                else
                {
                    stage.Updated++;
                }
            }
            catch (UserFriendlyException ex)
            {
                // The app service's own validation. Its message is already customer-readable.
                Fail(stage, rowNumber, code, ex.Message);
            }
            catch (ApplyRowException ex)
            {
                Fail(stage, rowNumber, code, ex.Message);
            }
        }

        return stage;
    }

    private static void Fail(GoLiveApplyStageDto stage, int row, string key, string message)
    {
        stage.Failed++;

        if (stage.Issues.Count < MaxReportedIssues)
        {
            stage.Issues.Add(new GoLiveApplyIssueDto { Row = row, Key = key, Message = message });
        }
        else
        {
            stage.IssuesTruncated++;
        }
    }

    private CreateOrEditEmployeeDto BuildDto(
        Dictionary<string, JsonElement> row,
        string code,
        IReadOnlyDictionary<string, int> nationalities,
        IReadOnlyDictionary<string, int> existing)
    {
        var nameEn = GoLiveDraftReader.Text(row, "nameEn")
                     ?? throw new ApplyRowException("English name is missing.");
        var nameAr = GoLiveDraftReader.Text(row, "nameAr")
                     ?? throw new ApplyRowException("Arabic name is missing.");

        var nationalityName = GoLiveDraftReader.Text(row, "nationality")
                              ?? throw new ApplyRowException("Nationality is missing.");

        if (!nationalities.TryGetValue(nationalityName, out var nationalityId))
        {
            // Reference data is seeded, so an unmatched nationality means a typo the grid's dropdown
            // would normally have prevented -- usually a pasted value on an unseeded tenant.
            throw new ApplyRowException($"Nationality '{nationalityName}' is not in the system.");
        }

        var birthDate = GoLiveDraftReader.Date(row, "birthDate")
                        ?? throw new ApplyRowException("Date of birth is missing or not a date.");
        var joiningDate = GoLiveDraftReader.Date(row, "joiningDate")
                          ?? throw new ApplyRowException("Joining date is missing or not a date.");

        var dto = new CreateOrEditEmployeeDto
        {
            Code = code,
            NationalityId = nationalityId,
            BirthDate = birthDate,
            JoiningDate = joiningDate,
            Gender = ParseGender(GoLiveDraftReader.Text(row, "gender")),
            Status = IsYes(GoLiveDraftReader.Text(row, "active")) ? EmployeeStatus.Active : EmployeeStatus.Inactive,
            CivilID = ParseDigits(GoLiveDraftReader.Text(row, "civilId"), "Civil ID"),
            Mobile = ParseDigits(GoLiveDraftReader.Text(row, "mobile"), "Mobile"),
            Email = GoLiveDraftReader.Text(row, "email"),
        };

        ApplyNames(dto, nameEn, nameAr);

        if (existing.TryGetValue(code, out var id))
        {
            dto.Id = id;
        }

        return dto;
    }

    /// <summary>
    /// The sheet collects one full name per language; the entity stores parts and derives the full name
    /// from them. First word to First, the remainder to Last, which round-trips to the same full name.
    /// </summary>
    private static void ApplyNames(CreateOrEditEmployeeDto dto, string nameEn, string nameAr)
    {
        var (firstEn, lastEn) = SplitName(nameEn);
        var (firstAr, lastAr) = SplitName(nameAr);

        dto.FirstNameEn = firstEn;
        dto.LastNameEn = lastEn;
        dto.FirstNameAr = firstAr;
        dto.LastNameAr = lastAr;
    }

    private static (string First, string Last) SplitName(string full)
    {
        var parts = full.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length <= 1 ? (full, string.Empty) : (parts[0], string.Join(' ', parts.Skip(1)));
    }

    private static Gender ParseGender(string value)
    {
        return string.Equals(value, "Female", StringComparison.OrdinalIgnoreCase) ? Gender.Female : Gender.Male;
    }

    private static bool IsYes(string value)
    {
        // Anything other than an explicit "No" is treated as still employed: the column is required, and
        // defaulting an unreadable value to Inactive would quietly drop someone from payroll.
        return !string.Equals(value, "No", StringComparison.OrdinalIgnoreCase);
    }

    private static long ParseDigits(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            throw new ApplyRowException($"{field} '{value}' contains no digits.");
        }

        if (!long.TryParse(digits, out var parsed))
        {
            throw new ApplyRowException($"{field} '{value}' is too long to be a number.");
        }

        return parsed;
    }
}

/// <summary>A row-level problem. Caught by the stage, reported, and the run continues.</summary>
public class ApplyRowException : Exception
{
    public ApplyRowException(string message) : base(message)
    {
    }
}
