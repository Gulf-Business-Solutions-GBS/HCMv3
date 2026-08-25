using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.EntityHistory;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.Authorization.Users;
using HCMv3.Dto;
using HCMv3.GridReports.Administration.Dto;
using HCMv3.GridReports.Exporting;
using HCMv3.SystemSecurity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports.Administration;

/// <summary>
/// Audit Report analysis grid report -- port of legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/Administration/uscRptAuditReport.ascx(.cs)</c>. See the
/// input DTO remarks for the model adaptation (ABP Entity History replaces legacy's XPO audit trail).
/// This is a system-level report with no employee/RLS concept, exactly like legacy.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Analysis_AuditReport)]
public class AuditReportAppService : GridReportAppServiceBase, IAuditReportAppService
{
    private readonly IRepository<EntityChange, long> _entityChangeRepository;
    private readonly IRepository<EntityChangeSet, long> _entityChangeSetRepository;
    private readonly IRepository<EntityPropertyChange, long> _entityPropertyChangeRepository;
    private readonly IRepository<User, long> _userRepository;

    public AuditReportAppService(
        IRepository<EntityChange, long> entityChangeRepository,
        IRepository<EntityChangeSet, long> entityChangeSetRepository,
        IRepository<EntityPropertyChange, long> entityPropertyChangeRepository,
        IRepository<User, long> userRepository)
    {
        _entityChangeRepository = entityChangeRepository;
        _entityChangeSetRepository = entityChangeSetRepository;
        _entityPropertyChangeRepository = entityPropertyChangeRepository;
        _userRepository = userRepository;
    }

    protected override string LegacyScreenName => "uscRptAuditReport";

    protected override string ScreenCaption => "Audit Report";

    [HttpPost]
    public async Task<PagedResultDto<AuditReportRowDto>> GetAuditReport(GetAuditReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var totalCount = rows.Count;

        var pagedRows = rows.AsQueryable().OrderBy(input.Sorting).PageBy(input).ToList();

        await WriteExecutionLogAsync(ExecutionLogAction.VIEW, BuildAuditParameterString(input), totalCount);

        return new PagedResultDto<AuditReportRowDto>(totalCount, pagedRows);
    }

    [HttpPost]
    public async Task<FileDto> GetAuditReportToExcel(GetAuditReportInput input)
    {
        input.Normalize();

        var rows = await BuildRowsAsync(input);

        var orderedRows = rows.AsQueryable().OrderBy(input.Sorting).ToList();

        return await ExportToFileAsync("AuditReport.xlsx", orderedRows, GetExportColumns(), BuildAuditParameterString(input));
    }

    /// <summary>Legacy <c>GetParameterStr()</c>: the two dates plus the user name filter.</summary>
    private string BuildAuditParameterString(GetAuditReportInput input)
    {
        return BuildParameterString(
            input,
            input.AuditUserName.IsNullOrWhiteSpace() ? null : "UserName : " + input.AuditUserName);
    }

    /// <summary>
    /// Single source of truth for filtering AND row construction, shared by the paged read and the export.
    /// Ports legacy <c>Table1()</c>'s SQL over the closest available HCMv3 equivalent (Entity History).
    /// </summary>
    private async Task<List<AuditReportRowDto>> BuildRowsAsync(GetAuditReportInput input)
    {
        var fromDate = input.ResolveFromDate(Today);
        var toDate = input.ResolveToDate(Today).AddDays(1).AddTicks(-1);
        var userName = input.AuditUserName;
        var entityObjectName = input.EntityObjectName;

        var query = from change in _entityChangeRepository.GetAll()
                     join changeSet in _entityChangeSetRepository.GetAll() on change.EntityChangeSetId equals changeSet.Id
                     join user in _userRepository.GetAll() on changeSet.UserId equals user.Id into users
                     from user in users.DefaultIfEmpty()
                     where change.ChangeTime >= fromDate && change.ChangeTime <= toDate
                     select new { change, user };

        if (!userName.IsNullOrWhiteSpace())
        {
            query = query.Where(q => q.user != null && q.user.UserName == userName);
        }

        if (!entityObjectName.IsNullOrWhiteSpace())
        {
            query = query.Where(q => q.change.EntityTypeFullName != null && q.change.EntityTypeFullName.Contains(entityObjectName));
        }

        var changes = await query
            .OrderBy(q => q.change.ChangeTime)
            .Select(q => new
            {
                q.change.Id,
                q.change.ChangeTime,
                q.change.ChangeType,
                q.change.EntityTypeFullName,
                UserName = q.user != null ? q.user.UserName : null
            })
            .ToListAsync();

        if (changes.Count == 0)
        {
            return new List<AuditReportRowDto>();
        }

        var changeIds = changes.Select(c => c.Id).ToList();
        var propertyChanges = await _entityPropertyChangeRepository.GetAll()
            .Where(p => changeIds.Contains(p.EntityChangeId))
            .ToListAsync();

        var propertyChangesByChangeId = propertyChanges.ToLookup(p => p.EntityChangeId);

        var rows = new List<AuditReportRowDto>();

        foreach (var change in changes)
        {
            var entityObject = ShortEntityName(change.EntityTypeFullName);
            var actionType = change.ChangeType.ToString();
            var actionDate = change.ChangeTime.Date;
            var actionTime = change.ChangeTime.ToString("hh:mm tt", CultureInfo.InvariantCulture);

            var propChanges = propertyChangesByChangeId[change.Id].ToList();

            if (propChanges.Count == 0)
            {
                rows.Add(new AuditReportRowDto
                {
                    Id = change.Id,
                    UserName = change.UserName,
                    ActionDate = actionDate,
                    ActionTime = actionTime,
                    ActionType = actionType,
                    EntityObject = entityObject,
                    PropertyName = null,
                    OldValue = null,
                    NewValue = null
                });
            }
            else
            {
                foreach (var propertyChange in propChanges)
                {
                    rows.Add(new AuditReportRowDto
                    {
                        Id = propertyChange.Id,
                        UserName = change.UserName,
                        ActionDate = actionDate,
                        ActionTime = actionTime,
                        ActionType = actionType,
                        EntityObject = entityObject,
                        PropertyName = propertyChange.PropertyName,
                        OldValue = propertyChange.OriginalValue,
                        NewValue = propertyChange.NewValue
                    });
                }
            }
        }

        return rows;
    }

    /// <summary>Legacy took everything after the last '.' of the entity type name.</summary>
    private static string ShortEntityName(string entityTypeFullName)
    {
        if (entityTypeFullName.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        var lastDot = entityTypeFullName.LastIndexOf('.');
        return lastDot >= 0 ? entityTypeFullName.Substring(lastDot + 1) : entityTypeFullName;
    }

    private static IReadOnlyList<GridReportColumn<AuditReportRowDto>> GetExportColumns()
    {
        return new List<GridReportColumn<AuditReportRowDto>>
        {
            new("UserName", r => r.UserName),
            new("ActionDate", r => r.ActionDate),
            new("ActionTime", r => r.ActionTime),
            new("ActionType", r => r.ActionType),
            new("EntityObject", r => r.EntityObject),
            new("PropertyName", r => r.PropertyName),
            new("OldValue", r => r.OldValue),
            new("NewValue", r => r.NewValue)
        };
    }
}
