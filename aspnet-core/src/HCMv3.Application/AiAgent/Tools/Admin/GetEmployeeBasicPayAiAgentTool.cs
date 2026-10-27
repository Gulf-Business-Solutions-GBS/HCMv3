using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.AiAgent.Tools;

/// <summary>An employee's basic pay as the Basic Pay screen shows it: the records over time and one record's lines.</summary>
public class GetEmployeeBasicPayAiAgentTool : IAiAgentTool
{
    private readonly AiPayLookup _lookup;
    private readonly IEmployeeBasicPaysAppService _basicPays;
    private readonly IEmployeeBasicPayDetailsAppService _basicPayDetails;

    public GetEmployeeBasicPayAiAgentTool(
        AiPayLookup lookup,
        IEmployeeBasicPaysAppService basicPays,
        IEmployeeBasicPayDetailsAppService basicPayDetails)
    {
        _lookup = lookup;
        _basicPays = basicPays;
        _basicPayDetails = basicPayDetails;
    }

    public string Name => "get_employee_basic_pay";

    public string Description =>
        "HR administration: show an employee's basic pay -- each wage type line and amount on the record in effect on " +
        "a date (default today), the total, and the dates of their recent basic pay records. Needs the employee code.";

    public string RequiredPermission => AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPayDetails;

    public JsonObject BuildParameters() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["employeeCode"] = new JsonObject { ["type"] = "string", ["description"] = "The employee code, e.g. 12001." },
            ["asOfDate"] = new JsonObject { ["type"] = "string", ["description"] = "Optional date, yyyy-MM-dd. Default today." }
        },
        ["required"] = new JsonArray("employeeCode")
    };

    public async Task<object> ExecuteAsync(AiToolArguments arguments, AiToolContext context)
    {
        var (employee, candidates) = await _lookup.ResolveEmployeeAsync(arguments.GetString("employeeCode"));
        if (employee == null)
        {
            return new
            {
                status = "employee_not_found",
                instruction = "No employee has exactly that code. Ask the user which employee they mean.",
                similar = candidates.Select(AiPayLookup.Summarise)
            };
        }

        var asOf = arguments.GetDate("asOfDate") ?? Clock.Now.Date;

        var records = (await _basicPays.GetAll(new GetAllEmployeeBasicPaysInput { EmployeeId = employee.Id, MaxResultCount = 1000 })).Items;
        var current = records.FirstOrDefault(r => r.StartDate <= asOf && r.EndDate >= asOf);

        if (current == null)
        {
            return new
            {
                employee = AiPayLookup.Summarise(employee),
                asOfDate = asOf.ToString("yyyy-MM-dd"),
                note = records.Count == 0 ? "This employee has no basic pay records." : "No basic pay record covers that date.",
                records = records.Take(5).Select(Period)
            };
        }

        var lines = (await _basicPayDetails.GetAll(new GetAllEmployeeBasicPayDetailsInput { EmployeeBasicPayId = current.Id, MaxResultCount = 1000 })).Items;
        var wageTypes = (await _lookup.GetAllWageTypesAsync()).ToDictionary(w => w.Id);

        return new
        {
            employee = AiPayLookup.Summarise(employee),
            asOfDate = asOf.ToString("yyyy-MM-dd"),
            record = Period(current),
            lines = lines.OrderBy(l => wageTypes.TryGetValue(l.WageTypeId, out var w) ? w.PayrollOrder : int.MaxValue).Select(l => new
            {
                wageTypeCode = wageTypes.TryGetValue(l.WageTypeId, out var w) ? w.Code : null,
                wageTypeEn = w?.NameEn ?? l.WageTypeName,
                wageTypeAr = w?.NameAr,
                amount = AiPayLookup.FormatAmount(l.Amount),
                currency = l.CurrencyName
            }),
            total = AiPayLookup.FormatAmount(lines.Sum(l => l.Amount)),
            otherRecords = records.Where(r => r.Id != current.Id).Take(5).Select(Period)
        };
    }

    private static object Period(EmployeeBasicPayDto r) => new
    {
        startDate = r.StartDate.ToString("yyyy-MM-dd"),
        endDate = r.EndDate.Year >= 9999 ? "open" : r.EndDate.ToString("yyyy-MM-dd"),
        total = AiPayLookup.FormatAmount(r.Amount)
    };
}
