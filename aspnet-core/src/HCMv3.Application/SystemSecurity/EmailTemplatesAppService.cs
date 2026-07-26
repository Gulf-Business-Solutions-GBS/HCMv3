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
using HCMv3.SystemSecurity.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Application service for <see cref="EmailTemplate"/> (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.EmailTemplate). Legacy PerformValidation parity: auto Code,
/// required NameEn, unique NameEn/NameAr.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmailTemplates)]
public class EmailTemplatesAppService : HCMv3AppServiceBase, IEmailTemplatesAppService
{
    private readonly IRepository<EmailTemplate> _repository;

    public EmailTemplatesAppService(IRepository<EmailTemplate> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmailTemplateDto>> GetAll(GetAllEmailTemplatesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t =>
                t.Code.Contains(input.Filter) ||
                t.NameEn.Contains(input.Filter) ||
                t.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmailTemplateDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmailTemplateForEditOutput> GetEmailTemplateForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmailTemplateForEditOutput
        {
            EmailTemplate = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmailTemplateDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmailTemplates_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmailTemplates_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmailTemplateDto input)
    {
        var entity = MapToEntity(input, new EmailTemplate());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmailTemplates_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmailTemplateDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task AssignCodeIfEmptyAsync(EmailTemplate entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(t => t.Code != null && t.Code != "")
            .Select(t => t.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    private async Task ValidateAsync(EmailTemplate entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(t => t.Id != entity.Id && t.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(t => t.Id != entity.Id &&
                           (t.NameEn == entity.NameEn || t.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static EmailTemplate MapToEntity(CreateOrEditEmailTemplateDto input, EmailTemplate entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.SmtpProfile = input.SmtpProfile;
        entity.Subject = input.Subject;
        entity.EmailBodyHtml = input.EmailBodyHtml;
        entity.EmailBodyText = input.EmailBodyText;

        return entity;
    }

    private static EmailTemplateDto ToDto(EmailTemplate entity)
    {
        return new EmailTemplateDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            SmtpProfile = entity.SmtpProfile,
            Subject = entity.Subject,
            EmailBodyHtml = entity.EmailBodyHtml,
            EmailBodyText = entity.EmailBodyText
        };
    }

    private static CreateOrEditEmailTemplateDto ToCreateOrEditDto(EmailTemplate entity)
    {
        return new CreateOrEditEmailTemplateDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            SmtpProfile = entity.SmtpProfile,
            Subject = entity.Subject,
            EmailBodyHtml = entity.EmailBodyHtml,
            EmailBodyText = entity.EmailBodyText
        };
    }
}
