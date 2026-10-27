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
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for <see cref="TempBenefitType"/> (ported from legacy XAF TempBenefitType business
/// object). See <see cref="TempBenefitType"/> class remarks for the free-text-grid nature of this entity.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_TempBenefitTypes)]
public class TempBenefitTypesAppService : HCMv3AppServiceBase, ITempBenefitTypesAppService
{
    private readonly IRepository<TempBenefitType> _repository;

    public TempBenefitTypesAppService(IRepository<TempBenefitType> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TempBenefitTypeDto>> GetAll(GetAllTempBenefitTypesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t => t.Grade.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TempBenefitTypeDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetTempBenefitTypeForEditOutput> GetTempBenefitTypeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTempBenefitTypeForEditOutput
        {
            TempBenefitType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTempBenefitTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TempBenefitTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TempBenefitTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTempBenefitTypeDto input)
    {
        var entity = MapToEntity(input, new TempBenefitType());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_TempBenefitTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTempBenefitTypeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static TempBenefitType MapToEntity(CreateOrEditTempBenefitTypeDto input, TempBenefitType entity)
    {
        entity.Grade = input.Grade;
        entity.Ticket = input.Ticket;
        entity.SimCard = input.SimCard;
        entity.Phone = input.Phone;
        entity.Incentive = input.Incentive;
        entity.BonusSIP = input.BonusSIP;
        entity.Uniform = input.Uniform;
        entity.MedicalInsurance = input.MedicalInsurance;
        entity.LeaveDays = input.LeaveDays;
        entity.MedicalLeave = input.MedicalLeave;
        entity.MaternityLeave = input.MaternityLeave;
        entity.StaffDiscount = input.StaffDiscount;
        entity.SpecialAllowance = input.SpecialAllowance;
        entity.ManagerAllowance = input.ManagerAllowance;
        entity.CompanyAsset = input.CompanyAsset;
        entity.SchoolAllowance = input.SchoolAllowance;
        entity.NoticePeriod = input.NoticePeriod;
        entity.Indemnity = input.Indemnity;
        entity.Retirement = input.Retirement;
        entity.MinSalary = input.MinSalary;
        entity.MidSalary = input.MidSalary;
        entity.MaxSalary = input.MaxSalary;
        entity.TransportationAllowance = input.TransportationAllowance;
        entity.HousingAllowance = input.HousingAllowance;
        entity.OtherAllowance = input.OtherAllowance;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: Grade required (enforced via DTO <c>[Required]</c>), no
    /// duplicate Grade (legacy "Names already In Use").
    /// </summary>
    private async Task ValidateAsync(TempBenefitType entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(t => t.Id != entity.Id && t.Grade == entity.Grade);
        if (duplicate)
        {
            throw new UserFriendlyException(L("GradeAlreadyInUse"));
        }
    }

    private static TempBenefitTypeDto ToDto(TempBenefitType entity)
    {
        return new TempBenefitTypeDto
        {
            Id = entity.Id,
            Grade = entity.Grade,
            Ticket = entity.Ticket,
            SimCard = entity.SimCard,
            Phone = entity.Phone,
            Incentive = entity.Incentive,
            BonusSIP = entity.BonusSIP,
            Uniform = entity.Uniform,
            MedicalInsurance = entity.MedicalInsurance,
            LeaveDays = entity.LeaveDays,
            MedicalLeave = entity.MedicalLeave,
            MaternityLeave = entity.MaternityLeave,
            StaffDiscount = entity.StaffDiscount,
            SpecialAllowance = entity.SpecialAllowance,
            ManagerAllowance = entity.ManagerAllowance,
            CompanyAsset = entity.CompanyAsset,
            SchoolAllowance = entity.SchoolAllowance,
            NoticePeriod = entity.NoticePeriod,
            Indemnity = entity.Indemnity,
            Retirement = entity.Retirement,
            MinSalary = entity.MinSalary,
            MidSalary = entity.MidSalary,
            MaxSalary = entity.MaxSalary,
            TransportationAllowance = entity.TransportationAllowance,
            HousingAllowance = entity.HousingAllowance,
            OtherAllowance = entity.OtherAllowance
        };
    }

    private static CreateOrEditTempBenefitTypeDto ToCreateOrEditDto(TempBenefitType entity)
    {
        return new CreateOrEditTempBenefitTypeDto
        {
            Id = entity.Id,
            Grade = entity.Grade,
            Ticket = entity.Ticket,
            SimCard = entity.SimCard,
            Phone = entity.Phone,
            Incentive = entity.Incentive,
            BonusSIP = entity.BonusSIP,
            Uniform = entity.Uniform,
            MedicalInsurance = entity.MedicalInsurance,
            LeaveDays = entity.LeaveDays,
            MedicalLeave = entity.MedicalLeave,
            MaternityLeave = entity.MaternityLeave,
            StaffDiscount = entity.StaffDiscount,
            SpecialAllowance = entity.SpecialAllowance,
            ManagerAllowance = entity.ManagerAllowance,
            CompanyAsset = entity.CompanyAsset,
            SchoolAllowance = entity.SchoolAllowance,
            NoticePeriod = entity.NoticePeriod,
            Indemnity = entity.Indemnity,
            Retirement = entity.Retirement,
            MinSalary = entity.MinSalary,
            MidSalary = entity.MidSalary,
            MaxSalary = entity.MaxSalary,
            TransportationAllowance = entity.TransportationAllowance,
            HousingAllowance = entity.HousingAllowance,
            OtherAllowance = entity.OtherAllowance
        };
    }
}
