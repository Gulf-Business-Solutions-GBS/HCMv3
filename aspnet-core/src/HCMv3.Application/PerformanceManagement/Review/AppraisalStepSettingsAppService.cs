using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;
using HCMv3.PerformanceManagement.Review.Dto;
using HCMv3.Workflow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PerformanceManagement.Review;

/// <summary>
/// The "Appraisal WF Setup" screen (legacy <c>PARequest</c> list view and detail view).
///
/// <para>
/// Validation is legacy's (<c>PARequest.PerformValidation</c>, <c>PARequestRelationship.PerformValidation</c>):
/// a request master, a step above zero that the master actually defines, and each relationship type once.
/// One rule is added that legacy did not have -- a step may have only one row. Legacy allowed two and then
/// read whichever the database returned first, so two rows for a step meant the flags in force depended on
/// row order. That is never what anyone configuring it intended.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_AppraisalStepSettings)]
public class AppraisalStepSettingsAppService : HCMv3AppServiceBase, IAppraisalStepSettingsAppService
{
    private readonly IRepository<AppraisalStepSetting> _repository;
    private readonly IRepository<AppraisalStepSettingRelationship> _relationshipRepository;
    private readonly IRepository<RequestMaster> _requestMasterRepository;
    private readonly IRepository<RequestDetail> _requestDetailRepository;
    private readonly IRepository<RelationshipType> _relationshipTypeRepository;

    public AppraisalStepSettingsAppService(
        IRepository<AppraisalStepSetting> repository,
        IRepository<AppraisalStepSettingRelationship> relationshipRepository,
        IRepository<RequestMaster> requestMasterRepository,
        IRepository<RequestDetail> requestDetailRepository,
        IRepository<RelationshipType> relationshipTypeRepository)
    {
        _repository = repository;
        _relationshipRepository = relationshipRepository;
        _requestMasterRepository = requestMasterRepository;
        _requestDetailRepository = requestDetailRepository;
        _relationshipTypeRepository = relationshipTypeRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AppraisalStepSettingDto>> GetAll(GetAllAppraisalStepSettingsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.RequestMaster)
            .WhereIf(input.RequestMasterId.HasValue, e => e.RequestMasterId == input.RequestMasterId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var settingIds = items.Select(i => i.Id).ToList();
        var relationships = await _relationshipRepository.GetAll()
            .Include(r => r.RelationshipType)
            .Where(r => settingIds.Contains(r.AppraisalStepSettingId))
            .ToListAsync();

        return new PagedResultDto<AppraisalStepSettingDto>(
            totalCount,
            items.Select(i => ToDto(i, relationships.Where(r => r.AppraisalStepSettingId == i.Id))).ToList());
    }

    public async Task<GetAppraisalStepSettingForEditOutput> GetAppraisalStepSettingForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        var dto = ToCreateOrEditDto(entity);
        dto.RelationshipTypeIds = await _relationshipRepository.GetAll()
            .Where(r => r.AppraisalStepSettingId == entity.Id)
            .OrderBy(r => r.Id)
            .Select(r => r.RelationshipTypeId)
            .ToListAsync();

        return new GetAppraisalStepSettingForEditOutput { AppraisalStepSetting = dto };
    }

    public async Task<AppraisalStepSettingLookupsDto> GetLookups()
    {
        var masters = await _requestMasterRepository.GetAll()
            .OrderBy(m => m.Code)
            .Select(m => new AppraisalStepSettingRequestMasterDto
            {
                Id = m.Id,
                Code = m.Code,
                NameEn = m.NameEn,
                NameAr = m.NameAr
            })
            .ToListAsync();

        var steps = await _requestDetailRepository.GetAll()
            .Select(d => new { d.RequestMasterId, d.Step })
            .ToListAsync();

        foreach (var master in masters)
        {
            master.Steps = steps.Where(s => s.RequestMasterId == master.Id)
                .Select(s => s.Step)
                .Distinct()
                .OrderBy(s => s)
                .ToList();
        }

        var relationshipTypes = await _relationshipTypeRepository.GetAll()
            .OrderBy(r => r.NameEn)
            .Select(r => new AppraisalStepSettingRelationshipTypeDto
            {
                Id = r.Id,
                Code = r.Code,
                NameEn = r.NameEn,
                NameAr = r.NameAr
            })
            .ToListAsync();

        return new AppraisalStepSettingLookupsDto
        {
            RequestMasters = masters,
            RelationshipTypes = relationshipTypes
        };
    }

    public async Task CreateOrEdit(CreateOrEditAppraisalStepSettingDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_AppraisalStepSettings_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        foreach (var relationship in await _relationshipRepository.GetAll()
                     .Where(r => r.AppraisalStepSettingId == entity.Id)
                     .ToListAsync())
        {
            await _relationshipRepository.DeleteAsync(relationship);
        }

        await _repository.DeleteAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_AppraisalStepSettings_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAppraisalStepSettingDto input)
    {
        var entity = MapToEntity(input, new AppraisalStepSetting());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity, input.RelationshipTypeIds);

        entity.Id = await _repository.InsertAndGetIdAsync(entity);

        await SyncRelationshipsAsync(entity, input.RelationshipTypeIds);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_AppraisalStepSettings_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAppraisalStepSettingDto input)
    {
        var entity = await _repository.GetAsync(input.Id!.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity, input.RelationshipTypeIds);

        await _repository.UpdateAsync(entity);

        await SyncRelationshipsAsync(entity, input.RelationshipTypeIds);
    }

    private static AppraisalStepSetting MapToEntity(CreateOrEditAppraisalStepSettingDto input, AppraisalStepSetting entity)
    {
        entity.RequestMasterId = input.RequestMasterId;
        entity.Step = input.Step;
        entity.CanAddKpi = input.CanAddKpi;
        entity.CanAddPeer = input.CanAddPeer;
        entity.CanAddRatings = input.CanAddRatings;
        entity.CanViewPeerRatings = input.CanViewPeerRatings;
        entity.CanViewRatings = input.CanViewRatings;
        entity.CanPeerAddRatings = input.CanPeerAddRatings;
        entity.CanViewOverallAverageRating = input.CanViewOverallAverageRating;
        entity.CanEmployeeSelfRate = input.CanEmployeeSelfRate;
        entity.AutoAddManager = input.AutoAddManager;
        entity.AutoAddRelationShip = input.AutoAddRelationShip;
        entity.AutoSubmitOnLastFill = input.AutoSubmitOnLastFill;
        entity.AllowedPeerCount = input.AllowedPeerCount;

        return entity;
    }

    private async Task ValidateAsync(AppraisalStepSetting entity, List<int> relationshipTypeIds)
    {
        var requestMaster = await _requestMasterRepository.FirstOrDefaultAsync(entity.RequestMasterId);
        if (requestMaster == null)
        {
            throw new UserFriendlyException(L("RequestMasterCannotBeBlank"));
        }

        if (entity.Step <= 0)
        {
            throw new UserFriendlyException(L("StepShouldBeGreaterThanZero"));
        }

        var stepDefined = await _requestDetailRepository.GetAll()
            .AnyAsync(d => d.RequestMasterId == entity.RequestMasterId && d.Step == entity.Step);
        if (!stepDefined)
        {
            throw new UserFriendlyException(L("RequestMasterHasNotDefinedTheStep", requestMaster.NameEn, entity.Step));
        }

        var duplicateStep = await _repository.GetAll()
            .AnyAsync(s => s.Id != entity.Id && s.RequestMasterId == entity.RequestMasterId && s.Step == entity.Step);
        if (duplicateStep)
        {
            throw new UserFriendlyException(L("AppraisalStepSettingAlreadyExistsForStep", requestMaster.NameEn, entity.Step));
        }

        var repeated = (relationshipTypeIds ?? new List<int>())
            .GroupBy(id => id)
            .FirstOrDefault(g => g.Count() > 1);
        if (repeated != null)
        {
            var name = await _relationshipTypeRepository.GetAll()
                .Where(r => r.Id == repeated.Key)
                .Select(r => r.NameEn)
                .FirstOrDefaultAsync();

            throw new UserFriendlyException(L("RelationshipTypeIsAlreadyAssignedToStep", name ?? repeated.Key.ToString(), entity.Step));
        }
    }

    /// <summary>Makes the stored relationship rows match the list: removes what was taken off, adds what is new.</summary>
    private async Task SyncRelationshipsAsync(AppraisalStepSetting entity, List<int> relationshipTypeIds)
    {
        var wanted = (relationshipTypeIds ?? new List<int>()).Distinct().ToList();

        var existing = await _relationshipRepository.GetAll()
            .Where(r => r.AppraisalStepSettingId == entity.Id)
            .ToListAsync();

        foreach (var removed in existing.Where(r => !wanted.Contains(r.RelationshipTypeId)))
        {
            await _relationshipRepository.DeleteAsync(removed);
        }

        foreach (var typeId in wanted.Where(id => existing.All(r => r.RelationshipTypeId != id)))
        {
            await _relationshipRepository.InsertAsync(new AppraisalStepSettingRelationship
            {
                TenantId = entity.TenantId,
                AppraisalStepSettingId = entity.Id,
                RelationshipTypeId = typeId
            });
        }
    }

    private static AppraisalStepSettingDto ToDto(AppraisalStepSetting entity, IEnumerable<AppraisalStepSettingRelationship> relationships)
    {
        var isArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

        return new AppraisalStepSettingDto
        {
            Id = entity.Id,
            RequestMasterId = entity.RequestMasterId,
            RequestMasterNameEn = entity.RequestMaster?.NameEn,
            RequestMasterNameAr = entity.RequestMaster?.NameAr,
            Step = entity.Step,
            CanAddKpi = entity.CanAddKpi,
            CanAddPeer = entity.CanAddPeer,
            CanAddRatings = entity.CanAddRatings,
            CanViewPeerRatings = entity.CanViewPeerRatings,
            CanViewRatings = entity.CanViewRatings,
            CanPeerAddRatings = entity.CanPeerAddRatings,
            CanViewOverallAverageRating = entity.CanViewOverallAverageRating,
            CanEmployeeSelfRate = entity.CanEmployeeSelfRate,
            AutoAddManager = entity.AutoAddManager,
            AutoAddRelationShip = entity.AutoAddRelationShip,
            AutoSubmitOnLastFill = entity.AutoSubmitOnLastFill,
            AllowedPeerCount = entity.AllowedPeerCount,
            RelationshipTypeNames = string.Join(", ", relationships
                .OrderBy(r => r.Id)
                .Select(r => isArabic ? r.RelationshipType?.NameAr ?? r.RelationshipType?.NameEn : r.RelationshipType?.NameEn)
                .Where(n => !string.IsNullOrEmpty(n)))
        };
    }

    private static CreateOrEditAppraisalStepSettingDto ToCreateOrEditDto(AppraisalStepSetting entity)
    {
        return new CreateOrEditAppraisalStepSettingDto
        {
            Id = entity.Id,
            RequestMasterId = entity.RequestMasterId,
            Step = entity.Step,
            CanAddKpi = entity.CanAddKpi,
            CanAddPeer = entity.CanAddPeer,
            CanAddRatings = entity.CanAddRatings,
            CanViewPeerRatings = entity.CanViewPeerRatings,
            CanViewRatings = entity.CanViewRatings,
            CanPeerAddRatings = entity.CanPeerAddRatings,
            CanViewOverallAverageRating = entity.CanViewOverallAverageRating,
            CanEmployeeSelfRate = entity.CanEmployeeSelfRate,
            AutoAddManager = entity.AutoAddManager,
            AutoAddRelationShip = entity.AutoAddRelationShip,
            AutoSubmitOnLastFill = entity.AutoSubmitOnLastFill,
            AllowedPeerCount = entity.AllowedPeerCount
        };
    }
}
