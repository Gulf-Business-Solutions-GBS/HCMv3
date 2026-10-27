using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Authorization.Users;
using HCMv3.Dto;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Employees;

/// <summary>
/// Employee Action Change analysis grid report -- port of legacy
/// <c>uscRptEmployeeActionChange.ascx(.cs)</c>, method <c>Table1()</c> (lines 484-745).
///
/// ATYPICAL -- DO NOT COPY THIS SHAPE INTO OTHER REPORTS. Every other report in this family is one
/// query projected to one row per source record, so <c>BuildQueryAsync</c> can return
/// <c>IQueryable&lt;TRow&gt;</c> and the database does the paging. This one cannot: legacy branches on
/// <c>PersonalAction.Code</c> and, for CHANGE IN PAY / PROMOTION, emits ONE ROW PER WAGE TYPE derived
/// from two separate pay snapshots. The shared method here is therefore
/// <see cref="BuildRowsAsync"/>, which MATERIALISES every matching row; the paged read then sorts and
/// pages that list in memory. The important property is preserved -- the read and the export go
/// through the identical row builder, so the export cannot drift from the grid's filters.
///
/// Legacy branch table (<c>PersonalAction.Code</c> -> rows emitted):
/// <list type="bullet">
/// <item><description>4 (CHANGE IN PAY), 3 (PROMOTION): one row per wage type, comparing the latest pay
/// starting BEFORE the action against the pay starting in the action's calendar month (lines 528-573).</description></item>
/// <item><description>10 (JOB TITLE CHANGE): one row, previous vs next org assignment (lines 576-614).
/// The legacy condition is <c>actionCode == "10" || actionCode == "3"</c>, but "3" is already consumed
/// by the branch above, so the "3" alternative is DEAD -- not ported.</description></item>
/// <item><description>7 (SEPARATION): one row; legacy sets CreatedBy but leaves CreatedOn/ModifiedOn/
/// ModifiedBy empty (lines 617-630), which is reproduced.</description></item>
/// <item><description>8 (FULL &amp; FINAL): one row. Legacy loads the matching
/// <c>EmployeeFnFPayment</c> (lines 633-636) and then never reads it -- a dead lookup, not ported.</description></item>
/// <item><description>9 (CONTRACT RENEWAL): one row, previous vs next contract end date (lines 665-701).</description></item>
/// <item><description>1 Joining, 2 Confirmation, 5 Suspension, 6 Transfer: one row with a hardcoded
/// Component label (lines 703-744).</description></item>
/// <item><description>Any other code: NO rows -- legacy has no else branch.</description></item>
/// </list>
///
/// NOT PORTED (DevExpress/WebForms UI plumbing): the drill-up/drill-down session stack, the
/// ColumnChooser JSON persistence, the callback-panel state machine and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeActionChange)]
public class EmployeeActionChangeReportAppService : GridReportAppServiceBase, IEmployeeActionChangeReportAppService
{
    private const string CodeJoining = "1";
    private const string CodeConfirmation = "2";
    private const string CodePromotion = "3";
    private const string CodeChangeInPay = "4";
    private const string CodeSuspension = "5";
    private const string CodeTransfer = "6";
    private const string CodeSeparation = "7";
    private const string CodeFullAndFinal = "8";
    private const string CodeContractRenewal = "9";
    private const string CodeJobTitleChange = "10";

    private readonly IRepository<EmployeeAction> _employeeActionRepository;
    private readonly IRepository<EmployeeBasicPayDetail> _basicPayDetailRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeContract> _employeeContractRepository;
    private readonly IRepository<User, long> _userRepository;

    public EmployeeActionChangeReportAppService(
        IRepository<EmployeeAction> employeeActionRepository,
        IRepository<EmployeeBasicPayDetail> basicPayDetailRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeContract> employeeContractRepository,
        IRepository<User, long> userRepository)
    {
        _employeeActionRepository = employeeActionRepository;
        _basicPayDetailRepository = basicPayDetailRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _employeeContractRepository = employeeContractRepository;
        _userRepository = userRepository;
    }

    protected override string LegacyScreenName => "uscRptEmployeeActionChange";

    protected override string ScreenCaption => "Employee Action Change";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeActionChangeReportRowDto>> GetEmployeeActionChangeReport(
        GetEmployeeActionChangeReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var totalCount = rows.Count;

        var pagedRows = rows.AsQueryable()
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildChangeParameterString(input), totalCount);

        return new PagedResultDto<EmployeeActionChangeReportRowDto>(totalCount, pagedRows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeActionChangeReportToExcel(GetEmployeeActionChangeReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var orderedRows = rows.AsQueryable()
            .OrderBy(input.Sorting)
            .ToList();

        return await ExportToFileAsync(
            "EmployeeActionChange.xlsx",
            orderedRows,
            GetExportColumns(),
            BuildChangeParameterString(input));
    }

    /// <summary>
    /// Legacy <c>GetParameterStr()</c> (uscRptEmployeeActionChange.ascx.cs:472-479) logs only the
    /// personal action and the two dates -- this report has no employee-code/status filters.
    /// </summary>
    private string BuildChangeParameterString(GetEmployeeActionChangeReportInput input)
    {
        return BuildParameterString(
            input,
            input.PersonalActionCode.IsNullOrWhiteSpace() ? null : "Personal Action : " + input.PersonalActionCode);
    }

    /// <summary>
    /// Single source of truth for filtering AND row construction, shared by the paged read and the
    /// export. Ports legacy <c>Table1()</c> (uscRptEmployeeActionChange.ascx.cs:484-745).
    /// </summary>
    private async Task<List<EmployeeActionChangeReportRowDto>> BuildRowsAsync(
        GetEmployeeActionChangeReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var personalActionCode = input.PersonalActionCode;
        var hasPersonalActionFilter = !personalActionCode.IsNullOrWhiteSpace();

        var actions = await _employeeActionRepository.GetAll()
            // Legacy lines 512-514: !(StartDate > dtTo || EndDate < dtFrom). The legacy EndDate getter
            // returns StartDate, so this collapses to "StartDate falls inside the window".
            .Where(a => a.StartDate.Date <= toDate && a.StartDate.Date >= fromDate)
            // Legacy lines 516-519: PersonalAction.Code == cmbPersonalAction, only when one was picked.
            .WhereIf(hasPersonalActionFilter, a => a.PersonalAction.Code == personalActionCode)
            // Legacy line 521 ApplyRowLevelSecurity: an empty RLS list means "no restriction".
            .WhereIf(rlsEmployeeIds.Count > 0, a => rlsEmployeeIds.Contains(a.EmployeeId))
            .Select(a => new ActionInfo
            {
                EmployeeId = a.EmployeeId,
                EmployeeCode = a.Employee.Code,
                EmployeeName = a.Employee.FullNameEn,
                ActionCode = a.PersonalAction == null ? null : a.PersonalAction.Code,
                ActionTypeName = a.PersonalAction == null ? null : a.PersonalAction.NameEn,
                StartDate = a.StartDate,
                CreatorUserId = a.CreatorUserId,
                CreationTime = a.CreationTime,
                LastModifierUserId = a.LastModifierUserId,
                LastModificationTime = a.LastModificationTime
            })
            .ToListAsync();

        if (actions.Count == 0)
        {
            return new List<EmployeeActionChangeReportRowDto>();
        }

        var employeeIds = actions.Select(a => a.EmployeeId).Distinct().ToList();
        var userNames = await GetUserNamesAsync(actions);

        // The three per-branch data sets are loaded once for all actions rather than per action (legacy
        // issued one query per action inside the loop) -- same data, far fewer round trips.
        var payDetails = actions.Any(a => a.ActionCode == CodeChangeInPay || a.ActionCode == CodePromotion)
            ? await LoadPayDetailsAsync(employeeIds)
            : new List<PayDetailInfo>();

        var orgAssignments = actions.Any(a => a.ActionCode == CodeJobTitleChange)
            ? await LoadOrgAssignmentsAsync(employeeIds)
            : new List<OrgAssignmentInfo>();

        var contracts = actions.Any(a => a.ActionCode == CodeContractRenewal)
            ? await LoadContractsAsync(employeeIds)
            : new List<ContractInfo>();

        var rows = new List<EmployeeActionChangeReportRowDto>();

        foreach (var action in actions)
        {
            switch (action.ActionCode)
            {
                case CodeChangeInPay:
                case CodePromotion:
                    AddPayChangeRows(rows, action, payDetails, userNames);
                    break;

                case CodeJobTitleChange:
                    AddJobTitleChangeRow(rows, action, orgAssignments, userNames);
                    break;

                case CodeSeparation:
                    // Legacy lines 617-630: only CreatedBy is set on this branch.
                    var separationRow = NewRow(action, userNames, "Separation");
                    separationRow.CreatedOn = null;
                    separationRow.ModifiedOn = null;
                    separationRow.ModifiedBy = null;
                    rows.Add(separationRow);
                    break;

                case CodeFullAndFinal:
                    // Legacy lines 633-636 load the matching EmployeeFnFPayment and never use it.
                    rows.Add(NewRow(action, userNames, "FnF"));
                    break;

                case CodeContractRenewal:
                    AddContractRenewalRow(rows, action, contracts, userNames);
                    break;

                case CodeJoining:
                    rows.Add(NewRow(action, userNames, "Joining"));
                    break;

                case CodeConfirmation:
                    rows.Add(NewRow(action, userNames, "Confirmation"));
                    break;

                case CodeSuspension:
                    rows.Add(NewRow(action, userNames, "Suspension"));
                    break;

                case CodeTransfer:
                    rows.Add(NewRow(action, userNames, "Transfer"));
                    break;

                // Legacy has no else branch: any other action code produces no rows at all.
            }
        }

        return rows;
    }

    /// <summary>
    /// Legacy lines 528-573. One row per wage type present either in the latest pay starting BEFORE the
    /// action or in the pay starting in the action's calendar month. AdjustedValue is the DELTA.
    /// </summary>
    private static void AddPayChangeRows(
        ICollection<EmployeeActionChangeReportRowDto> rows,
        ActionInfo action,
        IReadOnlyCollection<PayDetailInfo> payDetails,
        IReadOnlyDictionary<long, string> userNames)
    {
        var employeePays = payDetails.Where(p => p.EmployeeId == action.EmployeeId).ToList();

        // Legacy: OrderByDescending(BasicPay.StartDate) then GroupBy(WageType), taking the group's first
        // element -- i.e. the most recent pay line per wage type strictly before the action.
        var oldByWageType = employeePays
            .Where(p => p.BasicPayStartDate < action.StartDate)
            .OrderByDescending(p => p.BasicPayStartDate)
            .GroupBy(p => p.WageTypeId)
            .ToDictionary(g => g.Key, g => g.First());

        var newByWageType = employeePays
            .Where(p => p.BasicPayStartDate.Year == action.StartDate.Year
                        && p.BasicPayStartDate.Month == action.StartDate.Month)
            .GroupBy(p => p.WageTypeId)
            .ToDictionary(g => g.Key, g => g.First());

        var wageTypeIds = oldByWageType.Keys.Union(newByWageType.Keys).ToList();

        foreach (var wageTypeId in wageTypeIds)
        {
            oldByWageType.TryGetValue(wageTypeId, out var oldPay);
            newByWageType.TryGetValue(wageTypeId, out var newPay);

            var oldAmount = oldPay?.Amount ?? 0;
            var newAmount = newPay?.Amount ?? 0;

            var row = NewRow(
                action,
                userNames,
                // Legacy line 553: "Unknown" when the wage type is missing.
                newPay?.WageTypeNameEn ?? oldPay?.WageTypeNameEn ?? "Unknown");

            row.OriginalValue = FormatNumber(oldAmount);
            row.AdjustedValue = FormatNumber(newAmount - oldAmount);
            row.FinalValue = FormatNumber(newAmount);

            rows.Add(row);
        }
    }

    /// <summary>
    /// Legacy lines 576-614. Note legacy puts the NEW job title in BOTH AdjustedValue and FinalValue.
    /// </summary>
    private static void AddJobTitleChangeRow(
        ICollection<EmployeeActionChangeReportRowDto> rows,
        ActionInfo action,
        IReadOnlyCollection<OrgAssignmentInfo> orgAssignments,
        IReadOnlyDictionary<long, string> userNames)
    {
        var employeeAssignments = orgAssignments.Where(o => o.EmployeeId == action.EmployeeId).ToList();

        var previous = employeeAssignments
            .Where(o => o.StartDate < action.StartDate)
            .OrderByDescending(o => o.StartDate)
            .FirstOrDefault();

        var next = employeeAssignments
            .Where(o => o.StartDate >= action.StartDate)
            .OrderBy(o => o.StartDate)
            .FirstOrDefault();

        var row = NewRow(action, userNames, "Job Title");
        row.OriginalValue = previous?.JobTitleNameEn ?? string.Empty;
        row.AdjustedValue = next?.JobTitleNameEn ?? string.Empty;
        row.FinalValue = next?.JobTitleNameEn ?? string.Empty;

        rows.Add(row);
    }

    /// <summary>
    /// Legacy lines 665-701. The three value columns hold the contract END DATE formatted "dd/MM/yyyy",
    /// and again the new value is written to both AdjustedValue and FinalValue.
    /// </summary>
    private static void AddContractRenewalRow(
        ICollection<EmployeeActionChangeReportRowDto> rows,
        ActionInfo action,
        IReadOnlyCollection<ContractInfo> contracts,
        IReadOnlyDictionary<long, string> userNames)
    {
        var employeeContracts = contracts.Where(c => c.EmployeeId == action.EmployeeId).ToList();

        var previous = employeeContracts
            .Where(c => c.StartDate < action.StartDate)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefault();

        var next = employeeContracts
            .Where(c => c.StartDate >= action.StartDate)
            .OrderBy(c => c.StartDate)
            .FirstOrDefault();

        var row = NewRow(action, userNames, "Contract");
        row.OriginalValue = FormatDate(previous?.EndDate);
        row.AdjustedValue = FormatDate(next?.EndDate);
        row.FinalValue = FormatDate(next?.EndDate);

        rows.Add(row);
    }

    private static EmployeeActionChangeReportRowDto NewRow(
        ActionInfo action,
        IReadOnlyDictionary<long, string> userNames,
        string component)
    {
        return new EmployeeActionChangeReportRowDto
        {
            // Legacy stored the EMPLOYEE oid in the OID key column -- see the DTO remarks.
            Id = action.EmployeeId,
            EmployeeCode = action.EmployeeCode,
            EmployeeName = action.EmployeeName,
            ActionTypeName = action.ActionTypeName,
            StartDate = action.StartDate,
            // Legacy EndDate == StartDate by definition -- see the DTO remarks.
            EndDate = action.StartDate,
            Component = component,
            CreatedBy = ResolveUserName(userNames, action.CreatorUserId),
            CreatedOn = action.CreationTime,
            ModifiedOn = action.LastModificationTime,
            ModifiedBy = ResolveUserName(userNames, action.LastModifierUserId)
        };
    }

    private static string ResolveUserName(IReadOnlyDictionary<long, string> userNames, long? userId)
    {
        if (!userId.HasValue)
        {
            return null;
        }

        return userNames.TryGetValue(userId.Value, out var userName) ? userName : null;
    }

    /// <summary>Legacy <c>SysCreator</c>/<c>SysModifier</c> were user NAMES; ABP stores user ids.</summary>
    private async Task<Dictionary<long, string>> GetUserNamesAsync(IEnumerable<ActionInfo> actions)
    {
        var userIds = actions
            .SelectMany(a => new[] { a.CreatorUserId, a.LastModifierUserId })
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return new Dictionary<long, string>();
        }

        return await _userRepository.GetAll()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName);
    }

    private Task<List<PayDetailInfo>> LoadPayDetailsAsync(ICollection<int> employeeIds)
    {
        return _basicPayDetailRepository.GetAll()
            .Where(p => employeeIds.Contains(p.EmployeeBasicPay.EmployeeId))
            .Select(p => new PayDetailInfo
            {
                EmployeeId = p.EmployeeBasicPay.EmployeeId,
                BasicPayStartDate = p.EmployeeBasicPay.StartDate,
                WageTypeId = p.WageTypeId,
                WageTypeNameEn = p.WageType == null ? null : p.WageType.NameEn,
                Amount = p.Amount
            })
            .ToListAsync();
    }

    private Task<List<OrgAssignmentInfo>> LoadOrgAssignmentsAsync(ICollection<int> employeeIds)
    {
        return _orgAssignmentRepository.GetAll()
            .Where(o => employeeIds.Contains(o.EmployeeId))
            .Select(o => new OrgAssignmentInfo
            {
                EmployeeId = o.EmployeeId,
                StartDate = o.StartDate,
                JobTitleNameEn = o.JobTitle == null ? null : o.JobTitle.NameEn
            })
            .ToListAsync();
    }

    private Task<List<ContractInfo>> LoadContractsAsync(ICollection<int> employeeIds)
    {
        return _employeeContractRepository.GetAll()
            .Where(c => employeeIds.Contains(c.EmployeeId))
            .Select(c => new ContractInfo
            {
                EmployeeId = c.EmployeeId,
                StartDate = c.StartDate,
                EndDate = c.EndDate
            })
            .ToListAsync();
    }

    private static string FormatNumber(double value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatDate(DateTime? value)
    {
        // Legacy lines 688-690: oldContract?.EndDate.ToString("dd/MM/yyyy") ?? "".
        return value?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    /// <summary>
    /// The exported column set, in legacy column order. Legacy DROPPED OriginalValue/AdjustedValue/
    /// FinalValue for action codes 7, 8, 1, 2, 5 and 6 (lines 498-508); here they are always exported
    /// (they are simply empty on those branches) so the export shape does not depend on the filter.
    /// </summary>
    private static IReadOnlyList<GridReportColumn<EmployeeActionChangeReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeActionChangeReportRowDto>>
        {
            new("EmployeeCode", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("ActionType", r => r.ActionTypeName),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate),
            new("Component", r => r.Component),
            new("OriginalValue", r => r.OriginalValue),
            new("AdjustedValue", r => r.AdjustedValue),
            new("FinalValue", r => r.FinalValue),
            new("CreatedBy", r => r.CreatedBy),
            new("CreatedOn", r => r.CreatedOn),
            new("ModifiedOn", r => r.ModifiedOn),
            new("ModifiedBy", r => r.ModifiedBy)
        };
    }

    private sealed class ActionInfo
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string EmployeeName { get; set; }

        public string ActionCode { get; set; }

        public string ActionTypeName { get; set; }

        public DateTime StartDate { get; set; }

        public long? CreatorUserId { get; set; }

        public DateTime CreationTime { get; set; }

        public long? LastModifierUserId { get; set; }

        public DateTime? LastModificationTime { get; set; }
    }

    private sealed class PayDetailInfo
    {
        public int EmployeeId { get; set; }

        public DateTime BasicPayStartDate { get; set; }

        public int WageTypeId { get; set; }

        public string WageTypeNameEn { get; set; }

        public double Amount { get; set; }
    }

    private sealed class OrgAssignmentInfo
    {
        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public string JobTitleNameEn { get; set; }
    }

    private sealed class ContractInfo
    {
        public int EmployeeId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }
    }
}
