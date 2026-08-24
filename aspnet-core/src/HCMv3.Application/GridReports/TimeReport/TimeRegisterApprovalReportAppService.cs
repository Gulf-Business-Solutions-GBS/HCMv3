using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.TimeReport;

/// <summary>
/// Time Register Approval grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscTimeRegisterApproval.ascx(.cs)</c>,
/// method <c>Table1()</c> (lines 409-505).
///
/// READ-ONLY, NOT AN ACTION SCREEN. Despite the name the legacy control has no persistence path
/// whatsoever: its method list is <c>Table1()</c>, the DevExpress callback/column-chooser plumbing and
/// the two export buttons -- no <c>CommitChanges</c>, no <c>ObjectSpace.CommitChanges()</c>, no update of
/// any <c>Approved*</c> field. The screen simply shows the raw and the APPROVED value of each deduction
/// side by side so a reviewer can compare them; approving itself happens elsewhere. It therefore fits the
/// read-only grid-report template unchanged.
///
/// AGREED DIVERGENCE -- RLS: legacy did NOT call <c>ApplyRowLevelSecurity</c> here (unlike its sibling
/// <c>uscRptTimeRegister</c>, line 570), so any user who could open the screen saw every employee's
/// deductions. That is a security hole, and the standing rule for this wave is to apply RLS everywhere,
/// so this port filters by <c>GetRlsEmployeeIdsAsync()</c>.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack (single level -- <c>GetTable()</c> defines only "Table1"), the ColumnChooser JSON persistence
/// (lines 732-782), the callback-panel state machine (lines 513-700) and the PDF export button
/// (<c>btnExportPDF_Click</c>, line 705 -- this wave targets Excel).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_TimeRegisterApproval)]
public class TimeRegisterApprovalReportAppService
    : GridReportAppServiceBase, ITimeRegisterApprovalReportAppService
{
    private readonly IRepository<TimeRegister> _timeRegisterRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;

    public TimeRegisterApprovalReportAppService(
        IRepository<TimeRegister> timeRegisterRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<AbsenceType> absenceTypeRepository)
    {
        _timeRegisterRepository = timeRegisterRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _absenceTypeRepository = absenceTypeRepository;
    }

    protected override string LegacyScreenName => "uscTimeRegisterApproval";

    protected override string ScreenCaption => "Time Register Approval";

    [HttpPost]
    public async Task<PagedResultDto<TimeRegisterApprovalReportRowDto>> GetTimeRegisterApprovalReport(
        GetTimeRegisterApprovalReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await WriteExecutionLogAsync(
            ExecutionLogAction.VIEW,
            BuildApprovalParameterString(input),
            totalCount);

        return new PagedResultDto<TimeRegisterApprovalReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetTimeRegisterApprovalReportToExcel(
        GetTimeRegisterApprovalReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered data set,
        // so exporting only the current page would be silent data loss.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        return await ExportToFileAsync(
            "TimeRegisterApproval.xlsx",
            rows,
            GetExportColumns(),
            BuildApprovalParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND projection, shared by the paged read and the export --
    /// this is what guarantees the export honours exactly the same filters as the grid.
    /// Ports legacy <c>Table1()</c> (uscTimeRegisterApproval.ascx.cs:411-445).
    /// </summary>
    private async Task<IQueryable<TimeRegisterApprovalReportRowDto>> BuildQueryAsync(
        GetTimeRegisterApprovalReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var payrollAreaCode = input.PayrollAreaCode;
        var hasPayrollAreaFilter = !payrollAreaCode.IsNullOrWhiteSpace();

        // Legacy line 422 keys the payroll-area lookup on DateTime.Now, NOT on the report window.
        var today = Today;

        var query = _timeRegisterRepository.GetAll()
            // Legacy lines 415-416: DayDate >= dtFrom && DayDate <= dtTo -- both ends inclusive.
            .Where(r => r.DayDate >= fromDate && r.DayDate <= toDate)
            // Legacy lines 419-426 (cmbPayrollArea): the employee must have an org assignment covering
            // TODAY whose PayrollArea carries the selected code.
            .WhereIf(hasPayrollAreaFilter, r => _orgAssignmentRepository.GetAll()
                .Any(o => o.EmployeeId == r.EmployeeId
                          && o.StartDate <= today
                          && o.EndDate >= today
                          && o.PayrollAreaId != null
                          && o.PayrollArea.Code == payrollAreaCode))
            // Legacy lines 428-433: employees.Contains(r.Employee.Code) over the comma-split text box.
            .WhereIf(employeeCodes.Count > 0, r => employeeCodes.Contains(r.Employee.Code))
            // NOT in legacy -- see the class remarks. An empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, r => rlsEmployeeIds.Contains(r.EmployeeId));

        return query.Select(r => new TimeRegisterApprovalReportRowDto
        {
            // Legacy "Oid" column (line 435). Exposed as Id because it is the row key, not a data column.
            Id = r.Id,
            EmployeeCode = r.Employee.Code,
            DayDate = r.DayDate,
            // Legacy line 438: DayDate.DayOfWeek.ToString() -- invariant English, never localized.
            WeekDay = r.DayDate.DayOfWeek.ToString(),
            LeaveDay = r.LeaveDay,
            // Legacy line 440: r.LeaveType != null ? r.LeaveType.NameEn : "" -- English even in Arabic.
            // HCMv3 keeps LeaveTypeId as a bare id, so the name comes from a correlated sub-select.
            LeaveType = r.LeaveTypeId == null
                ? string.Empty
                : _absenceTypeRepository.GetAll()
                    .Where(a => a.Id == r.LeaveTypeId.Value)
                    .Select(a => a.NameEn)
                    .FirstOrDefault(),
            DedLateInEarlyOutMinutes = r.DedLateInEarlyOutMins,
            AbsentDay = r.AbsentDay,
            ApprovedDedLateInEarlyOutMinutes = r.ApprovedDedLateInEarlyOutMins,
            ApprovedAbsentDay = r.ApprovedAbsentDay,
            Status = r.Status,
            Remarks = r.Remarks
        });
    }

    /// <summary>
    /// Legacy had no <c>GetParameterStr()</c> on this screen (its <c>GetTable()</c> writes no execution
    /// log at all). The base helper's string is used so the port logs the same shape as its siblings; the
    /// payroll area is appended because it is the one report-specific filter.
    /// </summary>
    private string BuildApprovalParameterString(GetTimeRegisterApprovalReportInput input)
    {
        return BuildParameterString(
            input,
            input.PayrollAreaCode.IsNullOrWhiteSpace() ? null : "Payroll Area : " + input.PayrollAreaCode);
    }

    /// <summary>
    /// The exported column set, in legacy column order (lines 461-475). The technical Oid key is omitted
    /// because it is a surrogate database id with no meaning to the reader.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<TimeRegisterApprovalReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<TimeRegisterApprovalReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("DayDate", r => r.DayDate),
            new("WeekDay", r => r.WeekDay),
            new("LeaveDay", r => r.LeaveDay),
            new("LeaveType", r => r.LeaveType),
            new("DedLateInEarlyOutMinutes", r => r.DedLateInEarlyOutMinutes),
            new("AbsentDay", r => r.AbsentDay),
            new("ApprovedDedLateInEarlyOutMinutes", r => r.ApprovedDedLateInEarlyOutMinutes),
            new("ApprovedAbsentDay", r => r.ApprovedAbsentDay),
            new("Status", r => r.Status.ToString()),
            new("Remarks", r => r.Remarks)
        };
    }
}
