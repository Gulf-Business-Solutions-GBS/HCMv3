using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.AccommodationManagement;
using HCMv3.Authorization;
using HCMv3.Dto;
using HCMv3.GridReports.AccommodationReport.Dto;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Exporting;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.AccommodationReport;

/// <summary>
/// Employee Accommodation analysis grid report -- port of legacy
/// <c>uscRptEmployeeAccommodation.ascx(.cs)</c>, whose entire data contract is <c>Table1()</c>
/// (lines 566-805).
///
/// Legacy produced one row per (EmployeeAccommodation, AccommodationServices) pair via a LEFT JOIN
/// (<c>servsGroup.DefaultIfEmpty()</c>) -- ported here with a <c>GroupJoin</c> + <c>SelectMany</c>.
///
/// NOT PORTED -- DevExpress/WebForms UI plumbing with no data meaning: the drill-up/drill-down session
/// stack, the ColumnChooser JSON persistence, the callback-panel state machine and the PDF export button.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_EmployeeAccommodation)]
public class EmployeeAccommodationReportAppService : GridReportAppServiceBase, IEmployeeAccommodationReportAppService
{
    private readonly IRepository<EmployeeAccommodation> _employeeAccommodationRepository;
    private readonly IRepository<AccommodationServices> _accommodationServicesRepository;
    private readonly IRepository<EmployeeOrgAssignment> _employeeOrgAssignmentRepository;
    private readonly EmployeeOrgLevelNameResolver _orgLevelNameResolver;

    public EmployeeAccommodationReportAppService(
        IRepository<EmployeeAccommodation> employeeAccommodationRepository,
        IRepository<AccommodationServices> accommodationServicesRepository,
        IRepository<EmployeeOrgAssignment> employeeOrgAssignmentRepository,
        EmployeeOrgLevelNameResolver orgLevelNameResolver)
    {
        _employeeAccommodationRepository = employeeAccommodationRepository;
        _accommodationServicesRepository = accommodationServicesRepository;
        _employeeOrgAssignmentRepository = employeeOrgAssignmentRepository;
        _orgLevelNameResolver = orgLevelNameResolver;
    }

    protected override string LegacyScreenName => "uscRptEmployeeAccommodation";

    protected override string ScreenCaption => "Employee Accommodation";

    [HttpPost]
    public async Task<PagedResultDto<EmployeeAccommodationReportRowDto>> GetEmployeeAccommodationReport(
        GetEmployeeAccommodationReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildParameterString(input), totalCount);

        return new PagedResultDto<EmployeeAccommodationReportRowDto>(totalCount, rows);
    }

    [HttpPost]
    public async Task<FileDto> GetEmployeeAccommodationReportToExcel(GetEmployeeAccommodationReportInput input)
    {
        input.Normalize();

        var query = await BuildQueryAsync(input);

        // Deliberately NOT PageBy: the legacy ASPxGridViewExporter exported the entire filtered set.
        var rows = await query
            .OrderBy(input.Sorting)
            .ToListAsync();

        await StampOrgLevelsAsync(rows);

        return await ExportToFileAsync(
            "EmployeeAccommodation.xlsx",
            rows,
            GetExportColumns(),
            BuildParameterString(input));
    }

    /// <summary>
    /// Single source of truth for filtering AND the scalar part of the projection, shared by the paged
    /// read and the export. Ports legacy <c>Table1()</c> lines 665-756; the OrgLevel1..7 block is stamped
    /// on afterwards by <see cref="StampOrgLevelsAsync"/>, which BOTH callers also run.
    /// </summary>
    private async Task<IQueryable<EmployeeAccommodationReportRowDto>> BuildQueryAsync(
        GetEmployeeAccommodationReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today);
        var employeeCodes = input.ResolveEmployeeCodes();
        var rlsEmployeeIds = await GetRlsEmployeeIdsAsync();
        var useArabicNames = UseArabicNames;
        var employeeStatus = input.EmployeeStatus;
        var accommodationTypeCode = input.AccommodationTypeCode;
        var serviceTypeCode = input.ServiceTypeCode;
        var today = Today;

        var servicesQuery = _accommodationServicesRepository.GetAll();

        var accommodations = _employeeAccommodationRepository.GetAll()
            // Legacy line 667: !(r.StartDate.Date > dtToValue.Date || r.EndDate.Date < dtFromValue.Date).
            .Where(a => a.StartDate.Date <= toDate && a.EndDate.Date >= fromDate)
            .WhereIf(employeeStatus.HasValue, a => a.Employee.Status == employeeStatus.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(accommodationTypeCode), a => a.AccommodationType.Code == accommodationTypeCode)
            .WhereIf(
                !string.IsNullOrWhiteSpace(serviceTypeCode),
                a => servicesQuery.Any(s => s.EmployeeAccommodationId == a.Id && s.AccommodationServiceType.Code == serviceTypeCode))
            .WhereIf(employeeCodes.Count > 0, a => employeeCodes.Contains(a.Employee.Code))
            .WhereIf(rlsEmployeeIds.Count > 0, a => rlsEmployeeIds.Contains(a.EmployeeId));

        var query =
            from a in accommodations
            join s in servicesQuery on a.Id equals s.EmployeeAccommodationId into serviceGroup
            from service in serviceGroup.DefaultIfEmpty()
            select new EmployeeAccommodationReportRowDto
            {
                // Legacy "OID" column -- the row key, not a data column.
                Id = a.Id,
                EmployeeCode = a.Employee.Code,
                EmployeeName = useArabicNames ? a.Employee.FullNameAr : a.Employee.FullNameEn,
                AccommodationCode = a.AccommodationType != null ? a.AccommodationType.Code : string.Empty,
                AccommodationTypeName = a.AccommodationType != null
                    ? (useArabicNames ? a.AccommodationType.NameAr : a.AccommodationType.NameEn)
                    : string.Empty,
                LocationName = a.Location != null ? (useArabicNames ? a.Location.NameAr : a.Location.NameEn) : string.Empty,
                LocationComments = a.Location != null ? a.Location.Comments : string.Empty,
                BuildingName = a.Building != null ? (useArabicNames ? a.Building.NameAr : a.Building.NameEn) : string.Empty,
                BuildingComments = a.Building != null ? a.Building.Comments : string.Empty,
                FloorName = a.Floor != null ? (useArabicNames ? a.Floor.NameAr : a.Floor.NameEn) : string.Empty,
                FloorComments = a.Floor != null ? a.Floor.Comments : string.Empty,
                FlatName = a.Flat != null ? (useArabicNames ? a.Flat.NameAr : a.Flat.NameEn) : string.Empty,
                FlatComments = a.Flat != null ? a.Flat.Comments : string.Empty,
                RoomName = a.Room != null ? (useArabicNames ? a.Room.NameAr : a.Room.NameEn) : string.Empty,
                RoomComments = a.Room != null ? a.Room.Comments : string.Empty,
                BedName = a.Bed != null ? (useArabicNames ? a.Bed.NameAr : a.Bed.NameEn) : string.Empty,
                BedComments = a.Bed != null ? a.Bed.Comments : string.Empty,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                // Legacy lines 699-701/748-749: the org assignment CURRENT AS OF TODAY, not the
                // accommodation window.
                OrganizationUnitId = _employeeOrgAssignmentRepository.GetAll()
                    .Where(o => o.EmployeeId == a.EmployeeId && o.StartDate.Date <= today && o.EndDate.Date >= today)
                    .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                    .Select(o => o.OrganizationUnitId)
                    .FirstOrDefault(),
                JobTitle = _employeeOrgAssignmentRepository.GetAll()
                    .Where(o => o.EmployeeId == a.EmployeeId && o.StartDate.Date <= today && o.EndDate.Date >= today && o.JobTitle != null)
                    .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                    .Select(o => useArabicNames ? o.JobTitle.NameAr : o.JobTitle.NameEn)
                    .FirstOrDefault(),
                WorkLocation = _employeeOrgAssignmentRepository.GetAll()
                    .Where(o => o.EmployeeId == a.EmployeeId && o.StartDate.Date <= today && o.EndDate.Date >= today && o.WorkLocation != null)
                    .OrderBy(o => o.StartDate).ThenBy(o => o.Id)
                    .Select(o => useArabicNames ? o.WorkLocation.NameAr : o.WorkLocation.NameEn)
                    .FirstOrDefault(),
                ServiceTypeName = service != null && service.AccommodationServiceType != null
                    ? (useArabicNames ? service.AccommodationServiceType.NameAr : service.AccommodationServiceType.NameEn)
                    : string.Empty,
                ServiceStartDate = service != null ? service.StartDate : (System.DateTime?)null,
                ServiceEndDate = service != null ? service.EndDate : (System.DateTime?)null,
                Comments = a.Comments
            };

        return query;
    }

    /// <summary>Fills OrgLevel1..OrgLevel7 on already-materialized rows. See <see cref="EmployeeOrgLevelNameResolver"/>.</summary>
    private async Task StampOrgLevelsAsync(List<EmployeeAccommodationReportRowDto> rows)
    {
        if (rows.Count == 0)
        {
            return;
        }

        var lookup = await _orgLevelNameResolver.BuildLookupAsync(UseArabicNames);

        foreach (var row in rows)
        {
            var levels = lookup.GetLevelNames(row.OrganizationUnitId);
            row.OrgLevel1 = levels[0];
            row.OrgLevel2 = levels[1];
            row.OrgLevel3 = levels[2];
            row.OrgLevel4 = levels[3];
            row.OrgLevel5 = levels[4];
            row.OrgLevel6 = levels[5];
            row.OrgLevel7 = levels[6];
        }
    }

    private static IReadOnlyList<GridReportColumn<EmployeeAccommodationReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<EmployeeAccommodationReportRowDto>>
        {
            new("Code", r => r.EmployeeCode),
            new("Employee", r => r.EmployeeName),
            new("AccommodationCode", r => r.AccommodationCode),
            new("AccommodationType", r => r.AccommodationTypeName),
            new("Location", r => r.LocationName),
            new("LocationComments", r => r.LocationComments),
            new("Building", r => r.BuildingName),
            new("BuildingComments", r => r.BuildingComments),
            new("Floor", r => r.FloorName),
            new("FloorComments", r => r.FloorComments),
            new("Flat", r => r.FlatName),
            new("FlatComments", r => r.FlatComments),
            new("Room", r => r.RoomName),
            new("RoomComments", r => r.RoomComments),
            new("Bed", r => r.BedName),
            new("BedComments", r => r.BedComments),
            new("StartDate", r => r.StartDate),
            new("EndDate", r => r.EndDate),
            new("JobTitle", r => r.JobTitle),
            new("WorkLocation", r => r.WorkLocation),
            new("OrgLevel1", r => r.OrgLevel1),
            new("OrgLevel2", r => r.OrgLevel2),
            new("OrgLevel3", r => r.OrgLevel3),
            new("OrgLevel4", r => r.OrgLevel4),
            new("OrgLevel5", r => r.OrgLevel5),
            new("OrgLevel6", r => r.OrgLevel6),
            new("OrgLevel7", r => r.OrgLevel7),
            new("ServiceType", r => r.ServiceTypeName),
            new("ServiceStartDate", r => r.ServiceStartDate),
            new("ServiceEndDate", r => r.ServiceEndDate),
            new("Comments", r => r.Comments)
        };
    }
}
