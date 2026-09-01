using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeDocumentMvmt"/> (ported from legacy XAF
/// EmployeeDocumentMvmt business object). Legacy has an overlap-validation block but it is commented
/// out (dead code) -- only blank-field validation is ported.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDocumentMvmts)]
public class EmployeeDocumentMvmtsAppService : HCMv3AppServiceBase, IEmployeeDocumentMvmtsAppService
{
    private readonly IRepository<EmployeeDocumentMvmt> _repository;
    private readonly IRepository<EmployeeDocument> _documentRepository;

    public EmployeeDocumentMvmtsAppService(
        IRepository<EmployeeDocumentMvmt> repository,
        IRepository<EmployeeDocument> documentRepository)
    {
        _repository = repository;
        _documentRepository = documentRepository;
    }

    /// <summary>
    /// HR's view of the movement log.
    ///
    /// <para>
    /// The <c>ApprovedOnly</c> filter is disabled here, following <c>EmployeeAbsencesAppService</c>. Since a
    /// self-service withdrawal request IS a row in this table, leaving the filter on would hide every
    /// pending request from the very people who have to physically hand the document over -- the exact
    /// information legacy lost by keeping requests in a separate table. Each row carries
    /// <c>WorkflowStatus</c>, so the list can badge the ones not yet approved.
    /// </para>
    /// </summary>
    [HttpPost]
    public async Task<PagedResultDto<EmployeeDocumentMvmtDto>> GetAll(GetAllEmployeeDocumentMvmtsInput input)
    {
        input.Normalize();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var query = _repository.GetAll()
                .Include(m => m.EmployeeDocument)
                .WhereIf(input.EmployeeDocumentId.HasValue, m => m.EmployeeDocumentId == input.EmployeeDocumentId.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(input.Sorting)
                .PageBy(input)
                .ToListAsync();

            return new PagedResultDto<EmployeeDocumentMvmtDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    public async Task<GetEmployeeDocumentMvmtForEditOutput> GetEmployeeDocumentMvmtForEdit(EntityDto input)
    {
        // Same reasoning as GetAll: a row listed for HR must also be openable by them.
        EmployeeDocumentMvmt entity;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            entity = await _repository.GetAsync(input.Id);
        }

        return new GetEmployeeDocumentMvmtForEditOutput
        {
            EmployeeDocumentMvmt = new CreateOrEditEmployeeDocumentMvmtDto
            {
                Id = entity.Id,
                EmployeeDocumentId = entity.EmployeeDocumentId,
                WithdrawDate = entity.WithdrawDate,
                ReturnDate = entity.ReturnDate,
                ActualReturnDate = entity.ActualReturnDate == default ? null : entity.ActualReturnDate,
                GuarantorId = entity.GuarantorId,
                WithdrawalReason = entity.WithdrawalReason,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeDocumentMvmtDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDocumentMvmts_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeDocumentMvmtDto input)
    {
        var entity = MapToEntity(input, new EmployeeDocumentMvmt());
        entity.TenantId = AbpSession.TenantId ?? 0;
        entity.EmployeeId = await GetDocumentOwnerIdAsync(input.EmployeeDocumentId);

        ValidateDates(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDocumentMvmts_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeDocumentMvmtDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);
        entity.EmployeeId = await GetDocumentOwnerIdAsync(input.EmployeeDocumentId);

        ValidateDates(entity);
        await _repository.UpdateAsync(entity);
    }

    /// <summary>
    /// The movement's own <c>EmployeeId</c> is always the document's owner -- never a value the caller
    /// supplies. It is stored on the row rather than reached through the document because self-service
    /// scoping filters on it (see <c>EmployeeDocumentMvmt.EmployeeId</c>), and a scoping column that a
    /// client could set is not a scoping column.
    /// </summary>
    private async Task<int> GetDocumentOwnerIdAsync(int employeeDocumentId)
    {
        var ownerId = await _documentRepository.GetAll()
            .Where(d => d.Id == employeeDocumentId)
            .Select(d => (int?)d.EmployeeId)
            .FirstOrDefaultAsync();

        if (!ownerId.HasValue)
        {
            throw new UserFriendlyException(L("EmployeeDocumentNotFound"));
        }

        return ownerId.Value;
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeDocumentMvmts_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private void ValidateDates(EmployeeDocumentMvmt entity)
    {
        if (entity.WithdrawDate == default)
        {
            throw new UserFriendlyException(L("RequiredField", L("StartDate")));
        }

        if (entity.ReturnDate == default)
        {
            throw new UserFriendlyException(L("RequiredField", L("EndDate")));
        }
    }

    private static EmployeeDocumentMvmt MapToEntity(CreateOrEditEmployeeDocumentMvmtDto input, EmployeeDocumentMvmt entity)
    {
        entity.EmployeeDocumentId = input.EmployeeDocumentId;
        entity.WithdrawDate = input.WithdrawDate.Date;
        entity.ReturnDate = input.ReturnDate.Date;
        entity.ActualReturnDate = input.ActualReturnDate?.Date ?? default;
        entity.GuarantorId = input.GuarantorId;
        entity.WithdrawalReason = input.WithdrawalReason;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeDocumentMvmtDto ToDto(EmployeeDocumentMvmt entity)
    {
        return new EmployeeDocumentMvmtDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeDocumentId = entity.EmployeeDocumentId,
            EmployeeDocumentNum = entity.EmployeeDocument?.DocumentNum,
            WithdrawDate = entity.WithdrawDate,
            ReturnDate = entity.ReturnDate,
            ActualReturnDate = entity.ActualReturnDate,
            GuarantorId = entity.GuarantorId,
            WithdrawalReason = entity.WithdrawalReason,
            Comments = entity.Comments,
            WorkflowStatus = entity.WorkflowStatus
        };
    }
}
