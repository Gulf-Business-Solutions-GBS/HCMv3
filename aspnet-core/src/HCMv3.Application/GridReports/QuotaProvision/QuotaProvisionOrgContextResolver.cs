using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using HCMv3.GridReports.Employees;
using HCMv3.PersonnelAdministration;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.QuotaProvision;

/// <summary>
/// Shared helper for the QuotaProvision grouping: resolves, for a set of employees "as of" a given date,
/// the same org-context columns the legacy <c>usc*.ascx.cs</c> controls in
/// <c>HCM10.Web/UserControls/Reports/Analysis/QuotaProvision</c> read straight off the (legacy, cached)
/// <c>Employee.OrgLevel1..7</c>/<c>CurrentJobtitle</c>/<c>CurrentCostCenter</c> fields.
///
/// HCMv3's <see cref="EmployeeOrgAssignment"/> is date-bound (no cached "current" snapshot on
/// <see cref="Employee"/> itself), so this loads the ONE <see cref="EmployeeOrgAssignment"/> row effective
/// "as of" the caller-supplied date per employee, then defers OrgLevel1..7 name resolution to the already
/// established <see cref="EmployeeOrgLevelNameResolver"/> (see its remarks for why that step cannot be
/// folded into the same EF projection).
///
/// Not a report's <c>BuildQueryAsync</c> itself -- a single reusable building block several reports in this
/// grouping call from their own <c>BuildQueryAsync</c>, exactly the way
/// <see cref="EmployeeOrgLevelNameResolver"/> itself is reused across the whole "Employees" wave.
/// </summary>
public class QuotaProvisionOrgContextResolver : ITransientDependency
{
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public QuotaProvisionOrgContextResolver(
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    /// <summary>
    /// Builds a per-employee lookup of org context as of <paramref name="asOfDate"/> for exactly the given
    /// <paramref name="employeeIds"/>. Employees with no assignment effective on that date resolve to all-empty
    /// org levels and null job title/cost center/payroll area -- same "" fallback legacy used for a missing
    /// OrgLevelN.
    /// </summary>
    public async Task<QuotaProvisionOrgContextLookup> BuildAsync(IReadOnlyCollection<int> employeeIds, System.DateTime asOfDate, bool useArabicNames)
    {
        var assignments = await _employeeOrgAssignmentRepository.GetAll()
            .Where(a => employeeIds.Contains(a.EmployeeId) && a.StartDate <= asOfDate && a.EndDate >= asOfDate)
            .Select(a => new
            {
                a.EmployeeId,
                a.OrganizationUnitId,
                JobTitleName = a.JobTitle == null ? null : (useArabicNames ? a.JobTitle.NameAr : a.JobTitle.NameEn),
                CostCenterName = a.CostCenter == null ? null : (useArabicNames ? a.CostCenter.NameAr : a.CostCenter.NameEn),
                CostCenterCode = a.CostCenter == null ? null : a.CostCenter.Code,
                CostCenterGroupName = a.CostCenter != null && a.CostCenter.CostCenterGroup != null
                    ? (useArabicNames ? a.CostCenter.CostCenterGroup.NameAr : a.CostCenter.CostCenterGroup.NameEn)
                    : null,
                PayrollAreaName = a.PayrollArea == null ? null : a.PayrollArea.Name,
                WorkLocationName = a.WorkLocation == null ? null : (useArabicNames ? a.WorkLocation.NameAr : a.WorkLocation.NameEn),
                a.EmployeeSubGroupId
            })
            .ToListAsync();

        var byEmployee = assignments
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First());

        var orgLevelLookup = await _orgLevelNameResolver.BuildLookupAsync(useArabicNames);

        var contexts = new Dictionary<int, QuotaProvisionOrgContext>();
        foreach (var employeeId in employeeIds)
        {
            byEmployee.TryGetValue(employeeId, out var assignment);
            var levels = orgLevelLookup.GetLevelNames(assignment?.OrganizationUnitId);

            contexts[employeeId] = new QuotaProvisionOrgContext
            {
                OrgLevel1 = levels[0],
                OrgLevel2 = levels[1],
                OrgLevel3 = levels[2],
                OrgLevel4 = levels[3],
                OrgLevel5 = levels[4],
                OrgLevel6 = levels[5],
                OrgLevel7 = levels[6],
                JobTitleName = assignment?.JobTitleName ?? string.Empty,
                CostCenterName = assignment?.CostCenterName ?? string.Empty,
                CostCenterCode = assignment?.CostCenterCode ?? string.Empty,
                CostCenterGroupName = assignment?.CostCenterGroupName ?? string.Empty,
                PayrollAreaName = assignment?.PayrollAreaName ?? string.Empty,
                WorkLocationName = assignment?.WorkLocationName ?? string.Empty,
                EmployeeSubGroupId = assignment?.EmployeeSubGroupId
            };
        }

        return new QuotaProvisionOrgContextLookup(contexts);
    }
}

public class QuotaProvisionOrgContext
{
    public string OrgLevel1 { get; set; }
    public string OrgLevel2 { get; set; }
    public string OrgLevel3 { get; set; }
    public string OrgLevel4 { get; set; }
    public string OrgLevel5 { get; set; }
    public string OrgLevel6 { get; set; }
    public string OrgLevel7 { get; set; }
    public string JobTitleName { get; set; }
    public string CostCenterName { get; set; }
    public string CostCenterCode { get; set; }
    public string CostCenterGroupName { get; set; }
    public string PayrollAreaName { get; set; }
    public string WorkLocationName { get; set; }
    public int? EmployeeSubGroupId { get; set; }
}

public class QuotaProvisionOrgContextLookup
{
    private readonly Dictionary<int, QuotaProvisionOrgContext> _contexts;

    public QuotaProvisionOrgContextLookup(Dictionary<int, QuotaProvisionOrgContext> contexts)
    {
        _contexts = contexts;
    }

    public QuotaProvisionOrgContext Get(int employeeId)
    {
        return _contexts.TryGetValue(employeeId, out var context) ? context : new QuotaProvisionOrgContext
        {
            OrgLevel1 = string.Empty,
            OrgLevel2 = string.Empty,
            OrgLevel3 = string.Empty,
            OrgLevel4 = string.Empty,
            OrgLevel5 = string.Empty,
            OrgLevel6 = string.Empty,
            OrgLevel7 = string.Empty,
            JobTitleName = string.Empty,
            CostCenterName = string.Empty,
            CostCenterCode = string.Empty,
            CostCenterGroupName = string.Empty,
            PayrollAreaName = string.Empty,
            WorkLocationName = string.Empty
        };
    }
}
