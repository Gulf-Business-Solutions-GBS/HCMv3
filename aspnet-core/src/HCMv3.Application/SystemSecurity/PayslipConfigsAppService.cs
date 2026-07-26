using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.SystemSecurity.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Application service for <see cref="PayslipConfig"/> (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.PayslipConfig). Legacy PerformValidation parity: EmailTemplate
/// required, and only one row allowed (singleton-style, enforced here rather than at the schema level).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_PayslipConfigs)]
public class PayslipConfigsAppService : HCMv3AppServiceBase, IPayslipConfigsAppService
{
    private readonly IRepository<PayslipConfig> _repository;

    public PayslipConfigsAppService(IRepository<PayslipConfig> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PayslipConfigDto>> GetAll(GetAllPayslipConfigsInput input)
    {
        var query = _repository.GetAll().Include(c => c.EmailTemplate);

        var totalCount = await query.CountAsync();

        var items = await query
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PayslipConfigDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetPayslipConfigForEditOutput> GetPayslipConfigForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetPayslipConfigForEditOutput
        {
            PayslipConfig = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditPayslipConfigDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_PayslipConfigs_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_PayslipConfigs_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPayslipConfigDto input)
    {
        await ValidateSingletonAsync(null);

        var entity = MapToEntity(input, new PayslipConfig());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_PayslipConfigs_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPayslipConfigDto input)
    {
        await ValidateSingletonAsync(input.Id);

        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy PerformValidation parity: "PayslipConfig already exists. Please edit it!" -- only one row
    /// is allowed across the tenant.
    /// </summary>
    private async Task ValidateSingletonAsync(int? currentId)
    {
        var anyOther = await _repository.GetAll()
            .AnyAsync(c => !currentId.HasValue || c.Id != currentId.Value);

        if (anyOther)
        {
            throw new UserFriendlyException(L("PayslipConfigAlreadyExists"));
        }
    }

    private static PayslipConfig MapToEntity(CreateOrEditPayslipConfigDto input, PayslipConfig entity)
    {
        entity.EmailTemplateId = input.EmailTemplateId;

        return entity;
    }

    private static PayslipConfigDto ToDto(PayslipConfig entity)
    {
        return new PayslipConfigDto
        {
            Id = entity.Id,
            EmailTemplateId = entity.EmailTemplateId,
            EmailTemplateNameEn = entity.EmailTemplate?.NameEn
        };
    }

    private static CreateOrEditPayslipConfigDto ToCreateOrEditDto(PayslipConfig entity)
    {
        return new CreateOrEditPayslipConfigDto
        {
            Id = entity.Id,
            EmailTemplateId = entity.EmailTemplateId
        };
    }
}
