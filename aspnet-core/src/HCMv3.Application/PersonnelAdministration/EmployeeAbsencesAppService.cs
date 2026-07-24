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
/// Application service for <see cref="EmployeeAbsence"/> (ported from legacy XAF EmployeeAbsence business
/// object, Wave 1 deferred bucket). Ports the essential active validations only (required fields via DTO
/// annotations, EndDate not earlier than StartDate, no-overlapping-absence-period for the same employee).
/// The legacy leave-quota-balance cascade, workflow integration, sick-leave-deduction-slab calculation, and
/// notification side effects are NOT ported here -- see class remarks on <see cref="EmployeeAbsence"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsences)]
public class EmployeeAbsencesAppService : HCMv3AppServiceBase, IEmployeeAbsencesAppService
{
    private readonly IRepository<EmployeeAbsence> _repository;

    public EmployeeAbsencesAppService(IRepository<EmployeeAbsence> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeAbsenceDto>> GetAll(GetAllEmployeeAbsencesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(a => a.Employee)
            .Include(a => a.AbsenceType)
            .Include(a => a.ExtAbsenceType)
            .WhereIf(input.EmployeeId.HasValue, a => a.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeAbsenceDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeAbsenceForEditOutput> GetEmployeeAbsenceForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeAbsenceForEditOutput
        {
            EmployeeAbsence = new CreateOrEditEmployeeAbsenceDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                AbsenceTypeId = entity.AbsenceTypeId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                HalfdayType = entity.HalfdayType,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                IsTravelling = entity.IsTravelling,
                LeavePaymentType = entity.LeavePaymentType,
                PaymentDate = entity.PaymentDate,
                AdvanceLeavePaid = entity.AdvanceLeavePaid,
                Comments = entity.Comments,
                ReturnDate = entity.ReturnDate,
                IsReturned = entity.IsReturned,
                ExtAbsenceTypeId = entity.ExtAbsenceTypeId,
                ExtStartDate = entity.ExtStartDate,
                ExtEndDate = entity.ExtEndDate
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeAbsenceDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsences_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeAbsenceDto input)
    {
        var entity = MapToEntity(input, new EmployeeAbsence());
        entity.TenantId = AbpSession.TenantId ?? 0;
        entity.CalendarDays = CalculateCalendarDays(entity.StartDate, entity.EndDate);
        entity.TotalLeaveDays = entity.CalendarDays;
        entity.InitialEndDate = entity.EndDate;

        await ValidateAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsences_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeAbsenceDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);
        entity.CalendarDays = CalculateCalendarDays(entity.StartDate, entity.EndDate);
        entity.TotalLeaveDays = entity.CalendarDays;

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsences_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateAsync(EmployeeAbsence entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlaps = await _repository.GetAll()
            .AnyAsync(a => a.Id != entity.Id
                           && a.EmployeeId == entity.EmployeeId
                           && a.StartDate.Date <= entity.EndDate.Date
                           && a.EndDate.Date >= entity.StartDate.Date);
        if (overlaps)
        {
            throw new UserFriendlyException(L("OverlapConflictFound"));
        }
    }

    private static double CalculateCalendarDays(System.DateTime startDate, System.DateTime endDate)
    {
        if (startDate == default || endDate == default)
        {
            return 0;
        }

        return (endDate - startDate).TotalDays + 1;
    }

    private static EmployeeAbsence MapToEntity(CreateOrEditEmployeeAbsenceDto input, EmployeeAbsence entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.AbsenceTypeId = input.AbsenceTypeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.HalfdayType = input.HalfdayType;
        entity.StartTime = input.StartTime;
        entity.EndTime = input.EndTime;
        entity.IsTravelling = input.IsTravelling;
        entity.LeavePaymentType = input.LeavePaymentType;
        entity.PaymentDate = input.PaymentDate;
        entity.AdvanceLeavePaid = input.AdvanceLeavePaid;
        entity.Comments = input.Comments;
        entity.ReturnDate = input.ReturnDate;
        entity.IsReturned = input.IsReturned;
        entity.ExtAbsenceTypeId = input.ExtAbsenceTypeId;
        entity.ExtStartDate = input.ExtStartDate;
        entity.ExtEndDate = input.ExtEndDate;
        return entity;
    }

    private static EmployeeAbsenceDto ToDto(EmployeeAbsence entity)
    {
        return new EmployeeAbsenceDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            AbsenceTypeId = entity.AbsenceTypeId,
            AbsenceTypeName = entity.AbsenceType?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            HalfdayType = entity.HalfdayType,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            AbsenceHours = entity.AbsenceHours,
            IsTravelling = entity.IsTravelling,
            CalendarDays = entity.CalendarDays,
            TotalLeaveDays = entity.TotalLeaveDays,
            LeavePaymentType = entity.LeavePaymentType,
            PaymentDate = entity.PaymentDate,
            AdvanceLeavePaid = entity.AdvanceLeavePaid,
            Comments = entity.Comments,
            ReturnDate = entity.ReturnDate,
            IsReturned = entity.IsReturned,
            InitialEndDate = entity.InitialEndDate,
            ExtAbsenceTypeId = entity.ExtAbsenceTypeId,
            ExtAbsenceTypeName = entity.ExtAbsenceType?.NameEn,
            ExtStartDate = entity.ExtStartDate,
            ExtEndDate = entity.ExtEndDate,
            ExtTotalLeaveDays = entity.ExtTotalLeaveDays
        };
    }
}
