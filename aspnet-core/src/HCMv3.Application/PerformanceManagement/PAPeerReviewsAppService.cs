using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PerformanceManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PerformanceManagement;

/// <summary>
/// Application service for <see cref="PAPeerReview"/>. <see cref="PAPeerReviewDto.Submitted"/> is a
/// DTO-only computed field mirroring the matching <see cref="PAPeerNominee.Submitted"/> flag (legacy
/// NonPersistent parity), not a stored column on <see cref="PAPeerReview"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms)]
public class PAPeerReviewsAppService : HCMv3AppServiceBase, IPAPeerReviewsAppService
{
    private readonly IRepository<PAPeerReview> _repository;
    private readonly IRepository<PAForm> _paFormRepository;
    private readonly IRepository<PAPeerNominee> _paPeerNomineeRepository;

    public PAPeerReviewsAppService(
        IRepository<PAPeerReview> repository,
        IRepository<PAForm> paFormRepository,
        IRepository<PAPeerNominee> paPeerNomineeRepository)
    {
        _repository = repository;
        _paFormRepository = paFormRepository;
        _paPeerNomineeRepository = paPeerNomineeRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PAPeerReviewDto>> GetAll(GetAllPAPeerReviewsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Peer)
            .Include(e => e.KpiInfo)
            .WhereIf(input.PAFormId.HasValue, e => e.PAFormId == input.PAFormId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var dtos = new List<PAPeerReviewDto>();
        foreach (var item in items)
        {
            dtos.Add(await ToDtoAsync(item));
        }

        return new PagedResultDto<PAPeerReviewDto>(totalCount, dtos);
    }

    public async Task<GetPAPeerReviewForEditOutput> GetPAPeerReviewForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetPAPeerReviewForEditOutput { PAPeerReview = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditPAPeerReviewDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms_Edit)]
    protected virtual async Task CreateAsync(CreateOrEditPAPeerReviewDto input)
    {
        var entity = MapToEntity(input, new PAPeerReview());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAForms_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPAPeerReviewDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static PAPeerReview MapToEntity(CreateOrEditPAPeerReviewDto input, PAPeerReview entity)
    {
        entity.PAFormId = input.PAFormId;
        entity.PeerId = input.PeerId;
        entity.KpiInfoId = input.KpiInfoId;
        entity.RatingId = input.RatingId;
        entity.Comments = input.Comments;

        return entity;
    }

    private async Task<PAPeerReviewDto> ToDtoAsync(PAPeerReview entity)
    {
        var submitted = false;

        var paForm = await _paFormRepository.FirstOrDefaultAsync(entity.PAFormId);
        if (paForm != null)
        {
            var nominee = await _paPeerNomineeRepository.FirstOrDefaultAsync(n =>
                n.EmployeeId == paForm.EmployeeId && n.PAPeriodId == paForm.PAPeriodId && n.PeerId == entity.PeerId);
            submitted = nominee?.Submitted ?? false;
        }

        return new PAPeerReviewDto
        {
            Id = entity.Id,
            PAFormId = entity.PAFormId,
            PeerId = entity.PeerId,
            PeerFullNameEn = entity.Peer?.FullNameEn,
            KpiInfoId = entity.KpiInfoId,
            KpiInfoName = entity.KpiInfo?.NameEn,
            RatingId = entity.RatingId,
            Comments = entity.Comments,
            Submitted = submitted
        };
    }

    private static CreateOrEditPAPeerReviewDto ToCreateOrEditDto(PAPeerReview entity)
    {
        return new CreateOrEditPAPeerReviewDto
        {
            Id = entity.Id,
            PAFormId = entity.PAFormId,
            PeerId = entity.PeerId,
            KpiInfoId = entity.KpiInfoId,
            RatingId = entity.RatingId,
            Comments = entity.Comments
        };
    }
}
