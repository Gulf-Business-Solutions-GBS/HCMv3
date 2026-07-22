using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the NumberSeries master (ported from legacy XAF NumberSeries business object).
/// The legacy entity has no NameEn/NameAr and does not auto-generate Code (Code is a required, user-supplied
/// value), and its only real validation (duplicate EmploymentType checks) is tied to the deferred
/// <c>EmploymentType</c> FK, so this service is plain CRUD with Data Annotation validation only.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_NumberSeries)]
public class NumberSeriesAppService : HCMv3AppServiceBase, INumberSeriesAppService
{
    private readonly IRepository<NumberSeries> _numberSeriesRepository;

    public NumberSeriesAppService(IRepository<NumberSeries> numberSeriesRepository)
    {
        _numberSeriesRepository = numberSeriesRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<NumberSeriesDto>> GetAll(GetAllNumberSeriesInput input)
    {
        input.Normalize();

        var query = _numberSeriesRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), n =>
                n.Code.Contains(input.Filter) ||
                (n.Name != null && n.Name.Contains(input.Filter)));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<NumberSeriesDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetNumberSeriesForEditOutput> GetNumberSeriesForEdit(EntityDto input)
    {
        var numberSeries = await _numberSeriesRepository.GetAsync(input.Id);

        return new GetNumberSeriesForEditOutput
        {
            NumberSeries = ToCreateOrEditDto(numberSeries)
        };
    }

    public async Task CreateOrEdit(CreateOrEditNumberSeriesDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_NumberSeries_Create)]
    protected virtual async Task CreateAsync(CreateOrEditNumberSeriesDto input)
    {
        var numberSeries = new NumberSeries
        {
            Code = input.Code,
            Name = input.Name,
            Start = input.Start,
            Prefix = input.Prefix,
            Formatting = input.Formatting,
            TenantId = AbpSession.TenantId ?? 0
        };

        await _numberSeriesRepository.InsertAsync(numberSeries);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_NumberSeries_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditNumberSeriesDto input)
    {
        var numberSeries = await _numberSeriesRepository.GetAsync(input.Id.Value);

        numberSeries.Code = input.Code;
        numberSeries.Name = input.Name;
        numberSeries.Start = input.Start;
        numberSeries.Prefix = input.Prefix;
        numberSeries.Formatting = input.Formatting;

        await _numberSeriesRepository.UpdateAsync(numberSeries);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_NumberSeries_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _numberSeriesRepository.DeleteAsync(input.Id);
    }

    private static NumberSeriesDto ToDto(NumberSeries numberSeries)
    {
        return new NumberSeriesDto
        {
            Id = numberSeries.Id,
            Code = numberSeries.Code,
            Name = numberSeries.Name,
            Start = numberSeries.Start,
            Prefix = numberSeries.Prefix,
            Formatting = numberSeries.Formatting
        };
    }

    private static CreateOrEditNumberSeriesDto ToCreateOrEditDto(NumberSeries numberSeries)
    {
        return new CreateOrEditNumberSeriesDto
        {
            Id = numberSeries.Id,
            Code = numberSeries.Code,
            Name = numberSeries.Name,
            Start = numberSeries.Start,
            Prefix = numberSeries.Prefix,
            Formatting = numberSeries.Formatting
        };
    }
}
