using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.ReportManagement.Dto;
using HCMv3.ReportManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ReportManagement;

/// <summary>
/// Report Distribution: schedule CRUD, manual triggering and run history.
///
/// See <see cref="ReportSchedule"/> for why recipients are stored as rules and why each slice is
/// filtered by the RECIPIENT's row-level-security scope rather than the author's.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Reports_Distribution)]
public class ReportSchedulesAppService : HCMv3AppServiceBase, IReportSchedulesAppService
{
    private readonly IRepository<ReportSchedule> _scheduleRepository;
    private readonly IRepository<ReportScheduleRun, long> _runRepository;
    private readonly IRepository<ReportScheduleRunSlice, long> _sliceRepository;
    private readonly IReportScheduleExecutor _executor;

    public ReportSchedulesAppService(
        IRepository<ReportSchedule> scheduleRepository,
        IRepository<ReportScheduleRun, long> runRepository,
        IRepository<ReportScheduleRunSlice, long> sliceRepository,
        IReportScheduleExecutor executor)
    {
        _scheduleRepository = scheduleRepository;
        _runRepository = runRepository;
        _sliceRepository = sliceRepository;
        _executor = executor;
    }

    [HttpPost]
    public async Task<PagedResultDto<ReportScheduleListDto>> GetAll(GetAllReportSchedulesInput input)
    {
        input.Normalize();

        var query = _scheduleRepository.GetAllIncluding(s => s.Recipients)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), s =>
                s.Name.Contains(input.Filter) || s.ReportKey.Contains(input.Filter))
            .WhereIf(input.IsActive.HasValue, s => s.IsActive == input.IsActive.Value)
            .WhereIf(input.ReportKind.HasValue, s => s.ReportKind == input.ReportKind.Value);

        var totalCount = await query.CountAsync();

        var schedules = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        // Last run status per schedule, fetched in one pass rather than per row.
        var scheduleIds = schedules.Select(s => s.Id).ToList();
        var lastRuns = await _runRepository.GetAll()
            .Where(r => scheduleIds.Contains(r.ReportScheduleId))
            .GroupBy(r => r.ReportScheduleId)
            .Select(g => new
            {
                ReportScheduleId = g.Key,
                Status = g.OrderByDescending(r => r.StartedAt).Select(r => r.Status).FirstOrDefault(),
            })
            .ToListAsync();

        var lastRunByScheduleId = lastRuns.ToDictionary(r => r.ReportScheduleId, r => r.Status);

        var items = schedules.Select(s => new ReportScheduleListDto
        {
            Id = s.Id,
            Name = s.Name,
            ReportKind = s.ReportKind,
            ReportKey = s.ReportKey,
            Format = s.Format,
            BurstDimension = s.BurstDimension,
            OccurrenceType = s.OccurrenceType,
            RecurringPattern = s.RecurringPattern,
            IsActive = s.IsActive,
            LastRunTime = s.LastRunTime,
            NextRunTime = s.NextRunTime,
            RecipientRuleCount = s.Recipients?.Count ?? 0,
            LastRunStatus = lastRunByScheduleId.TryGetValue(s.Id, out var status) ? status : (ReportRunStatus?)null,
        }).ToList();

        return new PagedResultDto<ReportScheduleListDto>(totalCount, items);
    }

    public async Task<GetReportScheduleForEditOutput> GetForEdit(EntityDto input)
    {
        var schedule = await _scheduleRepository.GetAllIncluding(
                s => s.Filters,
                s => s.Recipients,
                s => s.Destinations)
            .FirstOrDefaultAsync(s => s.Id == input.Id);

        if (schedule == null)
        {
            throw new UserFriendlyException(L("ReportScheduleNotFound"));
        }

        return new GetReportScheduleForEditOutput
        {
            ReportSchedule = new CreateOrEditReportScheduleDto
            {
                Id = schedule.Id,
                Name = schedule.Name,
                ReportKind = schedule.ReportKind,
                ReportKey = schedule.ReportKey,
                Format = schedule.Format,
                NotificationEmails = schedule.NotificationEmails,
                NotifyOnStart = schedule.NotifyOnStart,
                NotifyOnCompletion = schedule.NotifyOnCompletion,
                IsActive = schedule.IsActive,
                BurstDimension = schedule.BurstDimension,
                BurstOrganizationUnitTypeId = schedule.BurstOrganizationUnitTypeId,
                BurstParameterName = schedule.BurstParameterName,
                OccurrenceType = schedule.OccurrenceType,
                RecurringPattern = schedule.RecurringPattern,
                FirstOccurrence = schedule.FirstOccurrence,
                EndingOn = schedule.EndingOn,
                Filters = schedule.Filters.Select(f => new ReportScheduleFilterDto
                {
                    Id = f.Id,
                    ParameterName = f.ParameterName,
                    ParameterValue = f.ParameterValue,
                    RelativeDateToken = f.RelativeDateToken,
                }).ToList(),
                Recipients = schedule.Recipients.Select(r => new ReportScheduleRecipientDto
                {
                    Id = r.Id,
                    Rule = r.Rule,
                    RoleId = r.RoleId,
                    ScopeToBurstUnit = r.ScopeToBurstUnit,
                    EmailAddress = r.EmailAddress,
                    BypassRecipientScope = r.BypassRecipientScope,
                }).ToList(),
                Destinations = schedule.Destinations.Select(d => new ReportScheduleDestinationDto
                {
                    Id = d.Id,
                    Method = d.Method,
                    EmailSubjectTemplate = d.EmailSubjectTemplate,
                    EmailBodyTemplate = d.EmailBodyTemplate,
                    HostAddress = d.HostAddress,
                    FtpLogin = d.FtpLogin,
                    FtpPasswordSettingName = d.FtpPasswordSettingName,
                    UseSftp = d.UseSftp,
                    FtpPassiveMode = d.FtpPassiveMode,
                    RemotePath = d.RemotePath,
                    StorageFolder = d.StorageFolder,
                }).ToList(),
            },
        };
    }

    public async Task CreateOrEdit(CreateOrEditReportScheduleDto input)
    {
        Validate(input);
        await GuardBypassRecipientScopeAsync(input);

        if (input.Id.HasValue)
        {
            await PermissionChecker.AuthorizeAsync(AppPermissions.Pages_Reports_Distribution_Edit);
            await UpdateAsync(input);
        }
        else
        {
            await PermissionChecker.AuthorizeAsync(AppPermissions.Pages_Reports_Distribution_Create);
            await CreateAsync(input);
        }
    }

    /// <summary>
    /// Refuses <c>BypassRecipientScope</c> unless the caller holds the elevated permission.
    ///
    /// Checked in the APP SERVICE rather than only in the UI: this is the one flag in the module that
    /// hands somebody data they are not otherwise entitled to, and a hidden checkbox is not an
    /// authorization control.
    /// </summary>
    private async Task GuardBypassRecipientScopeAsync(CreateOrEditReportScheduleDto input)
    {
        var wantsBypass = input.Recipients?.Any(r => r.BypassRecipientScope) == true;
        if (!wantsBypass)
        {
            return;
        }

        if (!await PermissionChecker.IsGrantedAsync(AppPermissions.Pages_Reports_Distribution_BypassRecipientScope))
        {
            throw new AbpAuthorizationException(L("BypassRecipientScopeNotPermitted"));
        }

        // Even with the permission, bypassing only makes sense for an external address. Allowing it on
        // a role- or org-derived recipient would quietly widen an internal person's access.
        if (input.Recipients.Any(r => r.BypassRecipientScope && r.Rule != ReportRecipientRule.Explicit))
        {
            throw new UserFriendlyException(L("BypassRecipientScopeOnlyForExplicitRecipients"));
        }
    }

    private static void Validate(CreateOrEditReportScheduleDto input)
    {
        if (input.BurstDimension == ReportBurstDimension.OrganizationUnit &&
            !input.BurstOrganizationUnitTypeId.HasValue)
        {
            throw new UserFriendlyException("Select the organization level to burst by.");
        }

        if (input.OccurrenceType == ReportScheduleOccurrenceType.Recurring && !input.RecurringPattern.HasValue)
        {
            throw new UserFriendlyException("Select a recurring pattern.");
        }

        if (input.Recipients == null || input.Recipients.Count == 0)
        {
            // A schedule with no recipients renders files nobody receives. Better refused at save time
            // than discovered as an empty run log weeks later.
            throw new UserFriendlyException("Add at least one recipient rule.");
        }

        if (input.Destinations == null || input.Destinations.Count == 0)
        {
            throw new UserFriendlyException("Add at least one destination.");
        }
    }

    private async Task CreateAsync(CreateOrEditReportScheduleDto input)
    {
        var entity = new ReportSchedule();
        MapToEntity(input, entity);
        entity.TenantId = AbpSession.TenantId ?? 0;

        ReplaceChildren(input, entity);

        await _scheduleRepository.InsertAsync(entity);
    }

    private async Task UpdateAsync(CreateOrEditReportScheduleDto input)
    {
        var entity = await _scheduleRepository.GetAllIncluding(
                s => s.Filters,
                s => s.Recipients,
                s => s.Destinations)
            .FirstOrDefaultAsync(s => s.Id == input.Id.Value);

        if (entity == null)
        {
            throw new UserFriendlyException(L("ReportScheduleNotFound"));
        }

        MapToEntity(input, entity);
        ReplaceChildren(input, entity);

        await _scheduleRepository.UpdateAsync(entity);
    }

    private static void MapToEntity(CreateOrEditReportScheduleDto input, ReportSchedule entity)
    {
        entity.Name = input.Name;
        entity.ReportKind = input.ReportKind;
        entity.ReportKey = input.ReportKey;
        entity.Format = input.Format;
        entity.NotificationEmails = input.NotificationEmails;
        entity.NotifyOnStart = input.NotifyOnStart;
        entity.NotifyOnCompletion = input.NotifyOnCompletion;
        entity.IsActive = input.IsActive;
        entity.BurstDimension = input.BurstDimension;
        entity.BurstOrganizationUnitTypeId = input.BurstOrganizationUnitTypeId;
        entity.BurstParameterName = input.BurstParameterName;
        entity.OccurrenceType = input.OccurrenceType;
        entity.RecurringPattern = input.RecurringPattern;
        entity.FirstOccurrence = input.FirstOccurrence;
        entity.EndingOn = input.EndingOn;
    }

    /// <summary>
    /// Children are replaced wholesale rather than diffed. They are a small, fully client-owned set,
    /// and diffing recipient rules by id invites the failure where a removed rule survives and keeps
    /// mailing somebody.
    /// </summary>
    private void ReplaceChildren(CreateOrEditReportScheduleDto input, ReportSchedule entity)
    {
        var tenantId = AbpSession.TenantId ?? 0;

        entity.Filters.Clear();
        foreach (var f in input.Filters ?? new List<ReportScheduleFilterDto>())
        {
            entity.Filters.Add(new ReportScheduleFilter
            {
                TenantId = tenantId,
                ParameterName = f.ParameterName,
                ParameterValue = f.ParameterValue,
                RelativeDateToken = f.RelativeDateToken,
            });
        }

        entity.Recipients.Clear();
        foreach (var r in input.Recipients ?? new List<ReportScheduleRecipientDto>())
        {
            entity.Recipients.Add(new ReportScheduleRecipient
            {
                TenantId = tenantId,
                Rule = r.Rule,
                RoleId = r.RoleId,
                ScopeToBurstUnit = r.ScopeToBurstUnit,
                EmailAddress = r.EmailAddress,
                BypassRecipientScope = r.BypassRecipientScope,
            });
        }

        entity.Destinations.Clear();
        foreach (var d in input.Destinations ?? new List<ReportScheduleDestinationDto>())
        {
            entity.Destinations.Add(new ReportScheduleDestination
            {
                TenantId = tenantId,
                Method = d.Method,
                EmailSubjectTemplate = d.EmailSubjectTemplate,
                EmailBodyTemplate = d.EmailBodyTemplate,
                HostAddress = d.HostAddress,
                FtpLogin = d.FtpLogin,
                FtpPasswordSettingName = d.FtpPasswordSettingName,
                UseSftp = d.UseSftp,
                FtpPassiveMode = d.FtpPassiveMode,
                RemotePath = d.RemotePath,
                StorageFolder = d.StorageFolder,
            });
        }
    }

    [AbpAuthorize(AppPermissions.Pages_Reports_Distribution_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _scheduleRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Reports_Distribution_Create)]
    public async Task<int> Duplicate(EntityDto input)
    {
        var source = await GetForEdit(new EntityDto(input.Id));

        var copy = source.ReportSchedule;
        copy.Id = null;
        copy.Name = $"{copy.Name} (copy)";

        // A duplicate starts INACTIVE. Copying a live schedule and having it immediately begin mailing
        // people, before anybody has adjusted its filters, is a surprise nobody wants.
        copy.IsActive = false;

        await CreateAsync(copy);
        await CurrentUnitOfWork.SaveChangesAsync();

        return (await _scheduleRepository.GetAll()
            .Where(s => s.Name == copy.Name)
            .OrderByDescending(s => s.Id)
            .FirstAsync()).Id;
    }

    [AbpAuthorize(AppPermissions.Pages_Reports_Distribution_Edit)]
    public async Task SetActive(SetReportScheduleActiveInput input)
    {
        var schedule = await _scheduleRepository.GetAsync(input.Id);
        schedule.IsActive = input.IsActive;
        await _scheduleRepository.UpdateAsync(schedule);
    }

    [AbpAuthorize(AppPermissions.Pages_Reports_Distribution_RunNow)]
    public async Task<RunReportScheduleOutput> RunNow(EntityDto input)
    {
        var result = await _executor.ExecuteAsync(input.Id, isManual: true);

        return new RunReportScheduleOutput
        {
            RunId = result.RunId,
            SliceCount = result.SliceCount,
            RecipientCount = result.RecipientCount,
            Status = result.Status,
            Message = result.Message,
        };
    }

    [HttpPost]
    public async Task<PagedResultDto<ReportScheduleRunDto>> GetRuns(GetReportScheduleRunsInput input)
    {
        input.Normalize();

        var query = _runRepository.GetAllIncluding(r => r.ReportSchedule)
            .WhereIf(input.ReportScheduleId.HasValue, r => r.ReportScheduleId == input.ReportScheduleId.Value)
            .WhereIf(input.Status.HasValue, r => r.Status == input.Status.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .Select(r => new ReportScheduleRunDto
            {
                Id = r.Id,
                ReportScheduleId = r.ReportScheduleId,
                ReportScheduleName = r.ReportSchedule.Name,
                StartedAt = r.StartedAt,
                FinishedAt = r.FinishedAt,
                Status = r.Status,
                IsManual = r.IsManual,
                SliceCount = r.SliceCount,
                ErrorMessage = r.ErrorMessage,
            })
            .ToListAsync();

        return new PagedResultDto<ReportScheduleRunDto>(totalCount, items);
    }

    public async Task<ListResultDto<ReportScheduleRunSliceDto>> GetRunSlices(EntityDto<long> input)
    {
        var items = await _sliceRepository.GetAll()
            .Where(s => s.ReportScheduleRunId == input.Id)
            .OrderBy(s => s.BurstKeyDisplay)
            .ThenBy(s => s.RecipientEmail)
            .Select(s => new ReportScheduleRunSliceDto
            {
                Id = s.Id,
                BurstKey = s.BurstKey,
                BurstKeyDisplay = s.BurstKeyDisplay,
                RecipientEmail = s.RecipientEmail,
                Method = s.Method,
                Status = s.Status,
                RowCount = s.RowCount,
                WasRecipientScoped = s.WasRecipientScoped,
                FileName = s.FileName,
                ErrorMessage = s.ErrorMessage,
            })
            .ToListAsync();

        return new ListResultDto<ReportScheduleRunSliceDto>(items);
    }
}
