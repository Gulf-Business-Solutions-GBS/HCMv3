using System.Collections.Generic;
using System.Linq;
using Abp.Domain.Repositories;
using Abp.Extensions;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Employee code generation: legacy <c>Employee.OnSaving</c> -&gt; <c>NumberSeries.GetCode(EmploymentType)</c>.
///
/// <para>
/// Which series applies, as legacy: if the tenant has an <c>ALL</c> <see cref="NumberSeries"/> it is used for
/// every employee and the next number is taken over ALL employee codes; otherwise the series whose
/// <see cref="NumberSeries.EmploymentType"/> matches the employee's, numbered over the codes of employees of that
/// employment type only. The next number is (highest numeric part of the codes that start with the series
/// <see cref="NumberSeries.Prefix"/>, prefix removed) + 1, formatted with <see cref="NumberSeries.Formatting"/>
/// (a .NET integer format such as <c>"0000"</c>) and prefixed again. Codes that are not numbers once the prefix is
/// removed are ignored. <see cref="NumberSeries.Start"/> is NOT used -- legacy <c>GetCode</c> never read it; the
/// first code of a new series is 1, formatted.
/// </para>
///
/// <para>
/// When no series applies, legacy refused to save ("NumberSeries not found for EmploymentType"). This port
/// keeps what HCMv3 did before series existed instead: (max existing numeric code) + 1 over all employees.
/// </para>
///
/// <para>
/// Legacy ran this in <c>Employee.OnSaving</c>, so it covered every way an employee was created; here it is
/// called explicitly by <c>EmployeesAppService</c> and by the recruitment Joined hand-off
/// (<c>ApplicationStatusesAppService</c>). Synchronous LINQ because HCMv3.Core does not use the EF Core async
/// extensions (see <c>EmployeeAbsenceCalculationService</c>).
/// </para>
/// </summary>
public class EmployeeCodeGenerator : HCMv3DomainServiceBase, IEmployeeCodeGenerator
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<NumberSeries> _numberSeriesRepository;

    public EmployeeCodeGenerator(
        IRepository<Employee> employeeRepository,
        IRepository<NumberSeries> numberSeriesRepository)
    {
        _employeeRepository = employeeRepository;
        _numberSeriesRepository = numberSeriesRepository;
    }

    public void AssignCodeIfEmpty(Employee employee)
    {
        if (!employee.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        employee.Code = NextCodeFromSeries(employee.EmploymentType) ?? NextPlainCode();
    }

    public void ReassignCodeIfEmploymentTypeChanged(Employee employee, EmploymentType previousEmploymentType)
    {
        if (employee.EmploymentType == previousEmploymentType)
        {
            return;
        }

        // Legacy threw when no series matched; with no series the employee simply keeps the code they have.
        var code = NextCodeFromSeries(employee.EmploymentType);
        if (code != null)
        {
            employee.Code = code;
        }
    }

    /// <summary>Legacy <c>NumberSeries.GetCode</c>; null when no series applies to <paramref name="employmentType"/>.</summary>
    private string NextCodeFromSeries(EmploymentType employmentType)
    {
        List<string> codes;

        var series = _numberSeriesRepository.GetAll().FirstOrDefault(s => s.EmploymentType == EmploymentType.ALL);
        if (series != null)
        {
            codes = _employeeRepository.GetAll().Select(e => e.Code).ToList();
        }
        else
        {
            series = _numberSeriesRepository.GetAll().FirstOrDefault(s => s.EmploymentType == employmentType);
            if (series == null)
            {
                return null;
            }

            codes = _employeeRepository.GetAll()
                .Where(e => e.EmploymentType == employmentType)
                .Select(e => e.Code)
                .ToList();
        }

        return FormatNextCode(series, codes);
    }

    private static string FormatNextCode(NumberSeries series, IEnumerable<string> codes)
    {
        // Legacy would have failed on an employee with no code; such rows simply do not count here.
        var numericParts = codes.Where(c => c != null);

        if (series.Prefix != null)
        {
            numericParts = numericParts
                .Where(c => c.StartsWith(series.Prefix))
                .Select(c => c.Substring(series.Prefix.Length));
        }

        var numbers = numericParts
            .Select(s => int.TryParse(s, out var n) ? (int?)n : null)
            .Where(n => n.HasValue)
            .Select(n => n.Value)
            .ToList();

        var next = (numbers.Count > 0 ? numbers.Max() : 0) + 1;

        return series.Prefix != null
            ? series.Prefix + next.ToString(series.Formatting)
            : next.ToString(series.Formatting);
    }

    /// <summary>
    /// What HCMv3 did before number series (legacy <c>GetMaxCode</c> pattern, as <c>CitiesAppService</c>): the
    /// highest numeric code + 1, over all employees.
    /// </summary>
    private string NextPlainCode()
    {
        var numericCodes = _employeeRepository.GetAll()
            .Where(e => e.Code != null && e.Code != "")
            .Select(e => e.Code)
            .ToList();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return (maxCode + 1).ToString();
    }
}
