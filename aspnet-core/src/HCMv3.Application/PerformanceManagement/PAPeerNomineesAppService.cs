using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PerformanceManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PerformanceManagement;

[AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAPeerNominees)]
public class PAPeerNomineesAppService : HCMv3AppServiceBase, IPAPeerNomineesAppService
{
    private readonly IRepository<PAPeerNominee> _repository;

    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _sysSettingsAccessor;

    public PAPeerNomineesAppService(
        IRepository<PAPeerNominee> repository,
        HCMv3.SystemSettings.Services.ISysSettingsAccessor sysSettingsAccessor)
    {
        _repository = repository;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    [HttpPost]
    public async Task<PagedResultDto<PAPeerNomineeDto>> GetAll(GetAllPAPeerNomineesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.PAPeriod)
            .Include(e => e.Employee)
            .Include(e => e.Peer)
            .WhereIf(input.PAPeriodId.HasValue, e => e.PAPeriodId == input.PAPeriodId)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PAPeerNomineeDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetPAPeerNomineeForEditOutput> GetPAPeerNomineeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return new GetPAPeerNomineeForEditOutput { PAPeerNominee = ToCreateOrEditDto(entity) };
    }

    public async Task CreateOrEdit(CreateOrEditPAPeerNomineeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAPeerNominees_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAPeerNominees_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPAPeerNomineeDto input)
    {
        var entity = new PAPeerNominee
        {
            TenantId = AbpSession.TenantId ?? 0,
            PAPeriodId = input.PAPeriodId,
            EmployeeId = input.EmployeeId,
            PeerId = input.PeerId,
            Submitted = input.Submitted
        };

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PerformanceManagement_PAPeerNominees_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPAPeerNomineeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.PAPeriodId = input.PAPeriodId;
        entity.EmployeeId = input.EmployeeId;
        entity.PeerId = input.PeerId;
        entity.Submitted = input.Submitted;

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task ValidateAsync(PAPeerNominee entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id
                           && e.PAPeriodId == entity.PAPeriodId
                           && e.EmployeeId == entity.EmployeeId
                           && e.PeerId == entity.PeerId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("PAPeerNomineeAlreadyExists"));
        }

        // Legacy ESS AppraisalRepository.HasReachedPeerReviewerLimit: with peer review on, an employee may
        // not be given more peers for a period than ManageAppraisalSettings.SetPeerMaxLimit (0 = no limit).
        var appraisalSettings = await _sysSettingsAccessor.GetAppraisalSettingsAsync();
        if (appraisalSettings != null && appraisalSettings.EnablePAPeerReview && appraisalSettings.SetPeerMaxLimit > 0)
        {
            var nominated = await _repository.GetAll()
                .CountAsync(e => e.Id != entity.Id
                                 && e.PAPeriodId == entity.PAPeriodId
                                 && e.EmployeeId == entity.EmployeeId);
            if (nominated >= appraisalSettings.SetPeerMaxLimit)
            {
                throw new UserFriendlyException(L("PeerReviewerLimitReached", appraisalSettings.SetPeerMaxLimit));
            }
        }
    }

    private static PAPeerNomineeDto ToDto(PAPeerNominee entity)
    {
        return new PAPeerNomineeDto
        {
            Id = entity.Id,
            PAPeriodId = entity.PAPeriodId,
            PAPeriodName = entity.PAPeriod?.Name,
            EmployeeId = entity.EmployeeId,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            PeerId = entity.PeerId,
            PeerFullNameEn = entity.Peer?.FullNameEn,
            Submitted = entity.Submitted
        };
    }

    private static CreateOrEditPAPeerNomineeDto ToCreateOrEditDto(PAPeerNominee entity)
    {
        return new CreateOrEditPAPeerNomineeDto
        {
            Id = entity.Id,
            PAPeriodId = entity.PAPeriodId,
            EmployeeId = entity.EmployeeId,
            PeerId = entity.PeerId,
            Submitted = entity.Submitted
        };
    }
}
