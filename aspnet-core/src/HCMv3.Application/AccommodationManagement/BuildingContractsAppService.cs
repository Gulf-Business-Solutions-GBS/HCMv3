using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.AccommodationManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.AccommodationManagement;

/// <summary>
/// Application service for the contract period of a <see cref="Building"/> (ported from legacy XAF
/// BuildingContract business object), listed separately and linked back via
/// <see cref="BuildingContract.BuildingId"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_AccommodationManagement_BuildingContracts)]
public class BuildingContractsAppService : HCMv3AppServiceBase, IBuildingContractsAppService
{
    private readonly IRepository<BuildingContract> _buildingContractRepository;

    public BuildingContractsAppService(IRepository<BuildingContract> buildingContractRepository)
    {
        _buildingContractRepository = buildingContractRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<BuildingContractDto>> GetAll(GetAllBuildingContractsInput input)
    {
        input.Normalize();

        var query = _buildingContractRepository.GetAll()
            .Include(c => c.Building)
            .WhereIf(input.BuildingId.HasValue, c => c.BuildingId == input.BuildingId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<BuildingContractDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetBuildingContractForEditOutput> GetBuildingContractForEdit(EntityDto input)
    {
        var entity = await _buildingContractRepository.GetAsync(input.Id);

        return new GetBuildingContractForEditOutput
        {
            BuildingContract = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditBuildingContractDto input)
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

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_BuildingContracts_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _buildingContractRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_BuildingContracts_Create)]
    protected virtual async Task CreateAsync(CreateOrEditBuildingContractDto input)
    {
        var entity = MapToEntity(input, new BuildingContract());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateAsync(entity);

        await _buildingContractRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_AccommodationManagement_BuildingContracts_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditBuildingContractDto input)
    {
        var entity = await _buildingContractRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateAsync(entity);

        await _buildingContractRepository.UpdateAsync(entity);
    }

    private static BuildingContract MapToEntity(CreateOrEditBuildingContractDto input, BuildingContract entity)
    {
        entity.BuildingId = input.BuildingId;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: EndDate &gt;= StartDate.
    /// </summary>
    private void ValidateAsync(BuildingContract entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static BuildingContractDto ToDto(BuildingContract entity)
    {
        return new BuildingContractDto
        {
            Id = entity.Id,
            BuildingId = entity.BuildingId,
            BuildingName = entity.Building?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditBuildingContractDto ToCreateOrEditDto(BuildingContract entity)
    {
        return new CreateOrEditBuildingContractDto
        {
            Id = entity.Id,
            BuildingId = entity.BuildingId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Comments = entity.Comments
        };
    }
}
