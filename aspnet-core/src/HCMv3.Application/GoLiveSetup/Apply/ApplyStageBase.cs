using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GoLiveSetup.Apply;

/// <summary>
/// The parts every Apply stage repeats: capped issue reporting, and resolving the employee code that
/// each of the S3.x sheets keys on.
/// </summary>
public abstract class ApplyStageBase
{
    /// <summary>Enough failures to diagnose the sheet without returning megabytes of them.</summary>
    private const int MaxReportedIssues = 50;

    /// <summary>Stands for an employee the draft will create but which does not exist yet.</summary>
    protected const int PendingEmployeeId = 0;

    protected static void Fail(GoLiveApplyStageDto stage, int row, string key, string message)
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

    /// <summary>
    /// Employee code to id for the whole tenant. Built once per stage: resolving per row would be a
    /// query each, and these sheets run to thousands of rows.
    ///
    /// <para>On a preview the employee stage has written nothing, so codes that only exist in the
    /// S3.1 draft are added with id 0 -- "will exist by the time this stage really runs". Without
    /// that, previewing a first-time go-live would report every bank and balance row as an unknown
    /// employee, which is precisely the run a consultant most needs a truthful preview of.</para>
    /// </summary>
    protected static async Task<Dictionary<string, int>> LoadEmployeeIdsAsync(
        IRepository<Employee> employees,
        GoLiveDraftReader reader)
    {
        var map = (await employees.GetAllListAsync())
            .Where(e => !string.IsNullOrWhiteSpace(e.Code))
            .GroupBy(e => e.Code.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        foreach (var row in await reader.ReadRowsAsync(EmployeeApplyStage.StepId))
        {
            var code = GoLiveDraftReader.Text(row, "code");
            if (code != null && !map.ContainsKey(code))
            {
                map[code] = PendingEmployeeId;
            }
        }

        return map;
    }

    /// <summary>
    /// The employee this row belongs to. Returns false and reports the reason when the row cannot be
    /// attributed -- which on a real sheet almost always means S3.1 has not been applied yet.
    /// </summary>
    protected static bool TryResolveEmployee(
        GoLiveApplyStageDto stage,
        IReadOnlyDictionary<string, int> employeeIds,
        Dictionary<string, JsonElement> row,
        int rowNumber,
        bool apply,
        out string code,
        out int employeeId)
    {
        employeeId = 0;
        code = GoLiveDraftReader.Text(row, "code");

        if (code == null)
        {
            Fail(stage, rowNumber, null, "Employee code is missing.");
            return false;
        }

        if (!employeeIds.TryGetValue(code, out employeeId))
        {
            Fail(stage, rowNumber, code, $"No employee with code '{code}'. Apply the employee sheet first.");
            return false;
        }

        // Only reachable on a real run if the employee stage failed this row, in which case the
        // dependent row cannot be written either.
        if (apply && employeeId == PendingEmployeeId)
        {
            Fail(stage, rowNumber, code, $"Employee '{code}' could not be created, so this row was skipped.");
            return false;
        }

        return true;
    }
}
