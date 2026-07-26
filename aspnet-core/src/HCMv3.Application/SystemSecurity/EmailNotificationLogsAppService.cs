using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.SystemSecurity.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Application service for <see cref="EmailNotificationLog"/> (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.EmailNotificationLog). Plain send-attempt audit-log CRUD.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmailNotificationLogs)]
public class EmailNotificationLogsAppService : HCMv3AppServiceBase, IEmailNotificationLogsAppService
{
    private readonly IRepository<EmailNotificationLog> _repository;

    public EmailNotificationLogsAppService(IRepository<EmailNotificationLog> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmailNotificationLogDto>> GetAll(GetAllEmailNotificationLogsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmailNotificationLogDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmailNotificationLogForEditOutput> GetEmailNotificationLogForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmailNotificationLogForEditOutput
        {
            EmailNotificationLog = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmailNotificationLogDto input)
    {
        if (input.Id.HasValue)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmailNotificationLogs_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmailNotificationLogs_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmailNotificationLogDto input)
    {
        var entity = MapToEntity(input, new EmailNotificationLog());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmailNotificationLogs_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmailNotificationLogDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static EmailNotificationLog MapToEntity(CreateOrEditEmailNotificationLogDto input, EmailNotificationLog entity)
    {
        entity.Date = input.Date;
        entity.Email = input.Email;
        entity.Source = input.Source;
        entity.Subject = input.Subject;
        entity.Body = input.Body;
        entity.IsSuccessful = input.IsSuccessful;
        entity.Exception = input.Exception;

        return entity;
    }

    private static EmailNotificationLogDto ToDto(EmailNotificationLog entity)
    {
        return new EmailNotificationLogDto
        {
            Id = entity.Id,
            Date = entity.Date,
            Email = entity.Email,
            Source = entity.Source,
            Subject = entity.Subject,
            Body = entity.Body,
            IsSuccessful = entity.IsSuccessful,
            Exception = entity.Exception
        };
    }

    private static CreateOrEditEmailNotificationLogDto ToCreateOrEditDto(EmailNotificationLog entity)
    {
        return new CreateOrEditEmailNotificationLogDto
        {
            Id = entity.Id,
            Date = entity.Date,
            Email = entity.Email,
            Source = entity.Source,
            Subject = entity.Subject,
            Body = entity.Body,
            IsSuccessful = entity.IsSuccessful,
            Exception = entity.Exception
        };
    }
}
