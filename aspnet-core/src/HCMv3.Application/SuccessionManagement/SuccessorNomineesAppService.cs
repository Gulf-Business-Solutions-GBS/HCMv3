using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.SuccessionManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SuccessionManagement;

/// <summary>
/// Application service for <see cref="SuccessorNominee"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SuccessionManagement.SuccessorNominee</c>). Ports the essential active
/// validation: required fields and no duplicate Employee+Nominee pair.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SuccessionManagement_SuccessorNominees)]
public class SuccessorNomineesAppService : HCMv3AppServiceBase, ISuccessorNomineesAppService
{
    private readonly IRepository<SuccessorNominee> _repository;

    public SuccessorNomineesAppService(IRepository<SuccessorNominee> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SuccessorNomineeDto>> GetAll(GetAllSuccessorNomineesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.Nominee)
            .Include(e => e.Readiness)
            .Include(e => e.Ranking)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SuccessorNomineeDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetSuccessorNomineeForEditOutput> GetSuccessorNomineeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSuccessorNomineeForEditOutput
        {
            SuccessorNominee = new CreateOrEditSuccessorNomineeDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                NomineeId = entity.NomineeId,
                ReadinessId = entity.ReadinessId,
                RankingId = entity.RankingId,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditSuccessorNomineeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SuccessionManagement_SuccessorNominees_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSuccessorNomineeDto input)
    {
        var entity = MapToEntity(input, new SuccessorNominee());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateEntityAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SuccessionManagement_SuccessorNominees_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSuccessorNomineeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await ValidateEntityAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SuccessionManagement_SuccessorNominees_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateEntityAsync(SuccessorNominee entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id && e.EmployeeId == entity.EmployeeId && e.NomineeId == entity.NomineeId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    private static SuccessorNominee MapToEntity(CreateOrEditSuccessorNomineeDto input, SuccessorNominee entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.NomineeId = input.NomineeId;
        entity.ReadinessId = input.ReadinessId;
        entity.RankingId = input.RankingId;
        entity.Comments = input.Comments;
        return entity;
    }

    private static SuccessorNomineeDto ToDto(SuccessorNominee entity)
    {
        return new SuccessorNomineeDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            NomineeId = entity.NomineeId,
            NomineeCode = entity.Nominee?.Code,
            NomineeFullNameEn = entity.Nominee?.FullNameEn,
            ReadinessId = entity.ReadinessId,
            ReadinessNameEn = entity.Readiness?.NameEn,
            RankingId = entity.RankingId,
            RankingNameEn = entity.Ranking?.NameEn,
            Comments = entity.Comments
        };
    }
}
