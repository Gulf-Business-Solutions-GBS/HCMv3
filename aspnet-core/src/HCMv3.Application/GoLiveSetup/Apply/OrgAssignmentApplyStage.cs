using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.GoLiveSetup.Apply;

/// <summary>
/// Attaches each employee to a payroll area, a job title and a work location, from the same S3.1 sheet
/// the employees themselves come from.
///
/// <para><b>This is what puts somebody into a payroll.</b> An employee with no org assignment has no
/// payroll area, so the engine never picks them up -- they exist, look correct on screen, and are
/// silently absent from the run. That failure mode is the reason this stage exists rather than the
/// employee sheet being considered done at <c>Employee</c>.</para>
/// </summary>
public class OrgAssignmentApplyStage : ApplyStageBase, ITransientDependency
{
    /// <summary>Reads the employee sheet; there is no separate assignment sheet to fill in.</summary>
    public const string StepId = "S3.1";

    private static readonly DateTime OpenEnded = new(9999, 12, 31);

    private readonly GoLiveDraftReader _reader;
    private readonly IEmployeeOrgAssignmentsAppService _assignments;
    private readonly IRepository<Employee> _employees;
    private readonly IRepository<EmployeeOrgAssignment> _assignmentRepository;
    private readonly IRepository<JobTitle> _jobTitles;
    private readonly IRepository<WorkLocation> _workLocations;
    private readonly IRepository<PayrollArea> _payrollAreas;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public OrgAssignmentApplyStage(
        GoLiveDraftReader reader,
        IEmployeeOrgAssignmentsAppService assignments,
        IRepository<Employee> employees,
        IRepository<EmployeeOrgAssignment> assignmentRepository,
        IRepository<JobTitle> jobTitles,
        IRepository<WorkLocation> workLocations,
        IRepository<PayrollArea> payrollAreas,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _reader = reader;
        _assignments = assignments;
        _employees = employees;
        _assignmentRepository = assignmentRepository;
        _jobTitles = jobTitles;
        _workLocations = workLocations;
        _payrollAreas = payrollAreas;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<GoLiveApplyStageDto> RunAsync(bool apply)
    {
        var stage = new GoLiveApplyStageDto { StepId = StepId, Target = nameof(EmployeeOrgAssignment) };

        var rows = await _reader.ReadRowsAsync(StepId);
        if (rows.Count == 0)
        {
            return stage;
        }

        var employeeIds = await LoadEmployeeIdsAsync(_employees, _reader);

        var jobTitles = (await _jobTitles.GetAllListAsync())
            .Where(j => !string.IsNullOrWhiteSpace(j.NameEn))
            .GroupBy(j => j.NameEn.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var workLocations = (await _workLocations.GetAllListAsync())
            .Where(w => !string.IsNullOrWhiteSpace(w.NameEn))
            .GroupBy(w => w.NameEn.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        // One payroll area is what the config stage creates, and what a single-company tenant needs.
        var payrollAreaId = (await _payrollAreas.GetAllListAsync()).FirstOrDefault()?.Id;

        var existing = (await _assignmentRepository.GetAllListAsync())
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First().Id);

        var joiningDates = (await _employees.GetAllListAsync())
            .ToDictionary(e => e.Id, e => e.JoiningDate);

        // An employee gets one assignment per run. Two rows with the same code are the same person, so
        // the second would otherwise build a second, overlapping assignment.
        //
        // Keyed on the CODE, not the resolved id: on a preview no employee has been written yet, so they
        // all resolve to PendingEmployeeId and keying on the id would collapse the whole sheet into one.
        var handled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (GoLiveDraftReader.IsEmpty(row))
            {
                continue;
            }

            var rowNumber = i + 1;

            // Unlike the other S3.x stages this reads the same sheet the employees come from, so a row
            // that cannot be attributed is one the employee stage has already reported. Reporting it a
            // second time would show the customer two failures for one bad row.
            var code = GoLiveDraftReader.Text(row, "code");
            if (code == null || !employeeIds.TryGetValue(code, out var employeeId) ||
                (apply && employeeId == PendingEmployeeId))
            {
                stage.Skipped++;
                continue;
            }

            if (!handled.Add(code))
            {
                stage.Skipped++;
                continue;
            }

            try
            {
                var jobTitleName = GoLiveDraftReader.Text(row, "jobTitle");
                int? jobTitleId = null;
                if (jobTitleName != null)
                {
                    if (!jobTitles.TryGetValue(jobTitleName, out var resolved))
                    {
                        throw new ApplyRowException($"Job title '{jobTitleName}' is not in the system.");
                    }

                    jobTitleId = resolved;
                }

                var locationName = GoLiveDraftReader.Text(row, "workLocation");
                int? workLocationId = null;
                if (locationName != null && workLocations.TryGetValue(locationName, out var location))
                {
                    // Unlike the job title this is optional on the sheet, so an unmatched name is left
                    // unset rather than failing the row.
                    workLocationId = location;
                }

                var isNew = !existing.TryGetValue(employeeId, out var assignmentId);

                var dto = new CreateOrEditEmployeeOrgAssignmentDto
                {
                    Id = isNew ? null : assignmentId,
                    EmployeeId = employeeId,
                    StartDate = joiningDates.TryGetValue(employeeId, out var joined) ? joined : DateTime.UtcNow.Date,
                    JobTitleId = jobTitleId,
                    WorkLocationId = workLocationId,
                };

                if (apply)
                {
                    await _assignments.CreateOrEdit(dto);

                    // EndDate and PayrollAreaId exist on the entity but not on its DTO -- the entity
                    // comment records that PayrollArea was deferred when this was ported and the DTO
                    // never caught up. Going through the app service first keeps its validation, then
                    // these two are set directly. Widening the DTO is the better fix, but it belongs to
                    // whoever owns that screen, not to a go-live importer.
                    await _unitOfWorkManager.Current.SaveChangesAsync();

                    // On an update this is the row CreateOrEdit just touched; on a create it is the one
                    // it just added. Re-querying by employee alone would pick an arbitrary row for an
                    // employee who already has assignment history.
                    var saved = isNew
                        ? await _assignmentRepository.FirstOrDefaultAsync(a => a.EmployeeId == employeeId)
                        : await _assignmentRepository.FirstOrDefaultAsync(assignmentId);
                    if (saved != null)
                    {
                        saved.EndDate = OpenEnded;
                        saved.PayrollAreaId = payrollAreaId;
                        await _assignmentRepository.UpdateAsync(saved);
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
                Fail(stage, rowNumber, code, ex.Message);
            }
            catch (ApplyRowException ex)
            {
                Fail(stage, rowNumber, code, ex.Message);
            }
        }

        return stage;
    }
}
