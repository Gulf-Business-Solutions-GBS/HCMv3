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
using HCMv3.Workflow.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Workflow;

/// <summary>
/// Application service for <see cref="RequestMaster"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Workflow.WorkflowSetup.RequestMaster</c>). Legacy
/// <c>PerformValidation</c> parity (auto Code, duplicate Code/Name checks) is ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_Workflow_RequestMasters)]
public class RequestMastersAppService : HCMv3AppServiceBase, IRequestMastersAppService
{
    private readonly IRepository<RequestMaster> _repository;

    public RequestMastersAppService(IRepository<RequestMaster> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<RequestMasterDto>> GetAll(GetAllRequestMastersInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), r =>
                r.Code.Contains(input.Filter) ||
                r.NameEn.Contains(input.Filter) ||
                r.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<RequestMasterDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetRequestMasterForEditOutput> GetRequestMasterForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetRequestMasterForEditOutput { RequestMaster = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditRequestMasterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestMasters_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestMasters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditRequestMasterDto input)
    {
        var entity = MapToEntity(input, new RequestMaster());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_RequestMasters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditRequestMasterDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task AssignCodeIfEmptyAsync(RequestMaster entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(r => r.Code != null && r.Code != "")
            .Select(r => r.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    private async Task ValidateUniquenessAsync(RequestMaster entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && r.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && (r.NameEn == entity.NameEn || r.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static RequestMaster MapToEntity(CreateOrEditRequestMasterDto input, RequestMaster entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.SecurityLevel = input.SecurityLevel;
        entity.AutoClose = input.AutoClose;
        entity.AutoCloseInDays = input.AutoCloseInDays;
        entity.DefaultApproverId = input.DefaultApproverId;
        entity.IsCommentRequire = input.IsCommentRequire;
        entity.DueInDays = input.DueInDays;
        return entity;
    }

    private static RequestMasterDto ToDto(RequestMaster entity)
    {
        return new RequestMasterDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            WorkflowSteps = entity.WorkflowSteps,
            SecurityLevel = entity.SecurityLevel,
            AutoClose = entity.AutoClose,
            AutoCloseInDays = entity.AutoCloseInDays,
            DefaultApproverId = entity.DefaultApproverId,
            IsCommentRequire = entity.IsCommentRequire,
            DueInDays = entity.DueInDays
        };
    }

    private static CreateOrEditRequestMasterDto ToCreateOrEditDto(RequestMaster entity)
    {
        return new CreateOrEditRequestMasterDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            SecurityLevel = entity.SecurityLevel,
            AutoClose = entity.AutoClose,
            AutoCloseInDays = entity.AutoCloseInDays,
            DefaultApproverId = entity.DefaultApproverId,
            IsCommentRequire = entity.IsCommentRequire,
            DueInDays = entity.DueInDays
        };
    }
}
