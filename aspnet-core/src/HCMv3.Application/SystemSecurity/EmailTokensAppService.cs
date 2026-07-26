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
/// Application service for <see cref="EmailToken"/> (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.EmailToken). Legacy PerformValidation parity: required
/// TokenName/TokenValue, unique TokenName.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmailTokens)]
public class EmailTokensAppService : HCMv3AppServiceBase, IEmailTokensAppService
{
    private readonly IRepository<EmailToken> _repository;

    public EmailTokensAppService(IRepository<EmailToken> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmailTokenDto>> GetAll(GetAllEmailTokensInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(t => t.EmailTemplate)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t => t.TokenName.Contains(input.Filter))
            .WhereIf(input.EmailTemplateId.HasValue, t => t.EmailTemplateId == input.EmailTemplateId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmailTokenDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetEmailTokenForEditOutput> GetEmailTokenForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmailTokenForEditOutput
        {
            EmailToken = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmailTokenDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmailTokens_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmailTokens_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmailTokenDto input)
    {
        var entity = MapToEntity(input, new EmailToken());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_EmailTokens_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmailTokenDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task ValidateAsync(EmailToken entity)
    {
        var duplicateName = await _repository.GetAll()
            .AnyAsync(t => t.Id != entity.Id && t.TokenName == entity.TokenName);
        if (duplicateName)
        {
            throw new UserFriendlyException(L("DuplicateTokenNameFound"));
        }
    }

    private static EmailToken MapToEntity(CreateOrEditEmailTokenDto input, EmailToken entity)
    {
        entity.TokenName = input.TokenName;
        entity.TokenValue = input.TokenValue;
        entity.IsDateTimeType = input.IsDateTimeType;
        entity.EmailTemplateId = input.EmailTemplateId;
        entity.ObjectKey = input.ObjectKey;

        return entity;
    }

    private static EmailTokenDto ToDto(EmailToken entity)
    {
        return new EmailTokenDto
        {
            Id = entity.Id,
            TokenName = entity.TokenName,
            TokenValue = entity.TokenValue,
            IsDateTimeType = entity.IsDateTimeType,
            EmailTemplateId = entity.EmailTemplateId,
            EmailTemplateNameEn = entity.EmailTemplate?.NameEn,
            ObjectKey = entity.ObjectKey
        };
    }

    private static CreateOrEditEmailTokenDto ToCreateOrEditDto(EmailToken entity)
    {
        return new CreateOrEditEmailTokenDto
        {
            Id = entity.Id,
            TokenName = entity.TokenName,
            TokenValue = entity.TokenValue,
            IsDateTimeType = entity.IsDateTimeType,
            EmailTemplateId = entity.EmailTemplateId,
            ObjectKey = entity.ObjectKey
        };
    }
}
