using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.ReportManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.ReportManagement;

/// <summary>
/// Application service for the selected entity/property nodes of a <see cref="ReportInfo"/> report
/// definition (ported from legacy XAF ReportInfoDetail business object). See
/// <see cref="ReportInfoDetail"/> class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportInfoDetails)]
public class ReportInfoDetailsAppService : HCMv3AppServiceBase, IReportInfoDetailsAppService
{
    private readonly IRepository<ReportInfoDetail> _repository;

    public ReportInfoDetailsAppService(IRepository<ReportInfoDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ReportInfoDetailDto>> GetAll(GetAllReportInfoDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(input.ReportInfoId.HasValue, d => d.ReportInfoId == input.ReportInfoId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ReportInfoDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetReportInfoDetailForEditOutput> GetReportInfoDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetReportInfoDetailForEditOutput
        {
            ReportInfoDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditReportInfoDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportInfoDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportInfoDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditReportInfoDetailDto input)
    {
        var entity = MapToEntity(input, new ReportInfoDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_ReportManagement_ReportInfoDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditReportInfoDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static ReportInfoDetail MapToEntity(CreateOrEditReportInfoDetailDto input, ReportInfoDetail entity)
    {
        entity.ReportInfoId = input.ReportInfoId;
        entity.NodeId = input.NodeId;
        entity.EntityName = input.EntityName;
        entity.NodeAlias = input.NodeAlias;
        entity.PropertyName = input.PropertyName;
        entity.PropertyDisplayName = input.PropertyDisplayName;
        entity.PropertyType = input.PropertyType;
        entity.BaseType = input.BaseType;
        entity.AssemblyName = input.AssemblyName;
        entity.ParentEntity = input.ParentEntity;
        entity.ParentAssembly = input.ParentAssembly;
        entity.IsAssociate = input.IsAssociate;
        entity.AssociateEntity = input.AssociateEntity;
        entity.AssociateProperty = input.AssociateProperty;

        return entity;
    }

    private static ReportInfoDetailDto ToDto(ReportInfoDetail entity)
    {
        return new ReportInfoDetailDto
        {
            Id = entity.Id,
            ReportInfoId = entity.ReportInfoId,
            NodeId = entity.NodeId,
            EntityName = entity.EntityName,
            NodeAlias = entity.NodeAlias,
            PropertyName = entity.PropertyName,
            PropertyDisplayName = entity.PropertyDisplayName,
            PropertyType = entity.PropertyType,
            BaseType = entity.BaseType,
            AssemblyName = entity.AssemblyName,
            ParentEntity = entity.ParentEntity,
            ParentAssembly = entity.ParentAssembly,
            IsAssociate = entity.IsAssociate,
            AssociateEntity = entity.AssociateEntity,
            AssociateProperty = entity.AssociateProperty
        };
    }

    private static CreateOrEditReportInfoDetailDto ToCreateOrEditDto(ReportInfoDetail entity)
    {
        return new CreateOrEditReportInfoDetailDto
        {
            Id = entity.Id,
            ReportInfoId = entity.ReportInfoId,
            NodeId = entity.NodeId,
            EntityName = entity.EntityName,
            NodeAlias = entity.NodeAlias,
            PropertyName = entity.PropertyName,
            PropertyDisplayName = entity.PropertyDisplayName,
            PropertyType = entity.PropertyType,
            BaseType = entity.BaseType,
            AssemblyName = entity.AssemblyName,
            ParentEntity = entity.ParentEntity,
            ParentAssembly = entity.ParentAssembly,
            IsAssociate = entity.IsAssociate,
            AssociateEntity = entity.AssociateEntity,
            AssociateProperty = entity.AssociateProperty
        };
    }
}
