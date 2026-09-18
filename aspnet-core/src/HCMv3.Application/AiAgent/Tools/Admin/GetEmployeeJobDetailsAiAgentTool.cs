using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Timing;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.AiAgent.Tools;

/// <summary>
/// "Who is 1002's manager?", "when does 1002's contract end?", "which department is 1002 in?" -- the manager,
/// contract and organisational assignment in effect on a date, each read through its own screen's service. A
/// section the user has no screen for is left out and said so, rather than failing the whole answer.
/// </summary>
public class GetEmployeeJobDetailsAiAgentTool : IAiAgentTool
{
    private readonly AiPayLookup _lookup;
    private readonly IEmployeeManagersAppService _managers;
    private readonly IEmployeeContractsAppService _contracts;
    private readonly IEmployeeOrgAssignmentsAppService _assignments;
    private readonly IPermissionChecker _permissionChecker;

    public GetEmployeeJobDetailsAiAgentTool(
        AiPayLookup lookup,
        IEmployeeManagersAppService managers,
        IEmployeeContractsAppService contracts,
        IEmployeeOrgAssignmentsAppService assignments,
        IPermissionChecker permissionChecker)
    {
        _lookup = lookup;
        _managers = managers;
        _contracts = contracts;
        _assignments = assignments;
        _permissionChecker = permissionChecker;
    }

    public string Name => "get_employee_job_details";

    public string Description =>
        "HR administration: an employee's manager, contract (type and dates) and organisational assignment " +
        "(unit, job title, cost center, work location) in effect on a date. Use before and after changing them.";

    /// <summary>Offered with the Employees screen; each section then needs its own screen.</summary>
    public string RequiredPermission => AppPermissions.Pages_PersonnelAdministration_Employees;

    public JsonObject BuildParameters() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["employeeCode"] = new JsonObject { ["type"] = "string" },
            ["asOfDate"] = new JsonObject { ["type"] = "string", ["description"] = "yyyy-MM-dd; leave out for today." }
        }
    };

    public async Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context)
    {
        var code = arguments.GetString("employeeCode");
        if (code == null)
        {
            return AiToolReplies.Ask("Ask the user which employee.");
        }

        var (employee, similar) = await _lookup.ResolveEmployeeAsync(code);
        if (employee == null)
        {
            return AiToolReplies.EmployeeNotFound(code, similar);
        }

        var asOf = (arguments.GetDate("asOfDate") ?? Clock.Now.Date).Date;
        const string noAccess = "not shown: the user does not have this screen";
        const string none = "none on this date";

        object manager = noAccess;
        if (await _permissionChecker.IsGrantedAsync(AppPermissions.Pages_PersonnelAdministration_EmployeeManagers))
        {
            var row = PrepareManagerChangeAiAgentTool.InEffect(
                (await _managers.GetAll(new GetAllEmployeeManagersInput { EmployeeId = employee.Id, MaxResultCount = 1000 })).Items, asOf);
            manager = row == null ? none : new { code = row.ManagerCode, nameEn = row.ManagerFullNameEn, since = Day(row.StartDate) };
        }

        object contract = noAccess;
        if (await _permissionChecker.IsGrantedAsync(AppPermissions.Pages_PersonnelAdministration_EmployeeContracts))
        {
            var row = PrepareContractChangeAiAgentTool.InEffect(
                (await _contracts.GetAll(new GetAllEmployeeContractsInput { EmployeeId = employee.Id, MaxResultCount = 1000 })).Items, asOf);
            contract = row == null ? none : new { type = row.ContractTypeName, from = Day(row.StartDate), to = Day(row.EndDate) };
        }

        object assignment = noAccess;
        if (await _permissionChecker.IsGrantedAsync(AppPermissions.Pages_PersonnelAdministration_EmployeeOrgAssignments))
        {
            var row = (await _assignments.GetAll(new GetAllEmployeeOrgAssignmentsInput { EmployeeId = employee.Id, MaxResultCount = 1000 })).Items
                .Where(r => r.StartDate.Date <= asOf && r.EndDate.Date >= asOf)
                .OrderByDescending(r => r.StartDate)
                .FirstOrDefault();
            assignment = row == null
                ? none
                : new
                {
                    orgUnit = row.OrganizationUnitName,
                    jobTitle = row.JobTitleName,
                    costCenter = row.CostCenterName,
                    workLocation = row.WorkLocationName,
                    since = Day(row.StartDate)
                };
        }

        return new
        {
            employee = AiPayLookup.Summarise(employee),
            asOfDate = asOf.ToString("yyyy-MM-dd"),
            manager,
            contract,
            orgAssignment = assignment
        };
    }

    /// <summary>The open end date reads as "open-ended", not as the year 9999.</summary>
    private static string Day(DateTime date) => date.Year >= 9999 ? "open-ended" : date.ToString("yyyy-MM-dd");
}
