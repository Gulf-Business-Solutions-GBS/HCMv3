using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.OrganizationManagement.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.OrganizationManagement;

/// <summary>
/// Port of legacy <c>HCM10.Module.Controllers.Organization.CostController</c> and the
/// <c>CostElementNList</c> data sources on <c>WageAccGroupDetail</c> / <c>CostRegisterDetail</c> /
/// <c>EmployeeWageAccount</c>: slot N is captioned with the cost element type whose Code is
/// "CostElementN", is hidden when no such type exists, and offers only cost elements of that type.
/// </summary>
/// <remarks>
/// Open to anyone who can see one of the screens that carry the slots, so a payroll user does not also
/// need the Cost Element Types / Cost Elements master permissions just to fill a row.
/// </remarks>
[AbpAuthorize(
    AppPermissions.Pages_PayrollManagement_WageAccGroupDetails,
    AppPermissions.Pages_PayrollManagement_CostRegisterDetails,
    AppPermissions.Pages_PayrollManagement_EmployeeWageAccounts,
    AppPermissions.Pages_PayrollManagement_WageAccounts)]
public class CostElementSlotsAppService : HCMv3AppServiceBase, ICostElementSlotsAppService
{
    public const int SlotCount = 7;

    private const string SlotCodePrefix = "CostElement";

    private readonly IRepository<CostElementType> _costElementTypeRepository;
    private readonly IRepository<CostElement> _costElementRepository;

    public CostElementSlotsAppService(
        IRepository<CostElementType> costElementTypeRepository,
        IRepository<CostElement> costElementRepository)
    {
        _costElementTypeRepository = costElementTypeRepository;
        _costElementRepository = costElementRepository;
    }

    public async Task<ListResultDto<CostElementSlotDto>> GetSlots()
    {
        var types = await _costElementTypeRepository.GetAll()
            .Where(t => t.Code != null && t.Code.StartsWith(SlotCodePrefix))
            .OrderBy(t => t.Id)
            .Select(t => new { t.Id, t.Code, t.NameEn, t.NameAr })
            .ToListAsync();

        var slots = new List<CostElementSlotDto>();
        for (var slot = 1; slot <= SlotCount; slot++)
        {
            var code = SlotCodePrefix + slot;
            // Lowest Id wins if a code was entered twice; legacy's ToDictionary would have thrown instead.
            var type = types.FirstOrDefault(t => string.Equals(t.Code.Trim(), code, StringComparison.OrdinalIgnoreCase));
            if (type == null)
            {
                continue;
            }

            slots.Add(new CostElementSlotDto
            {
                Slot = slot,
                CostElementTypeId = type.Id,
                Code = type.Code,
                NameEn = type.NameEn,
                NameAr = type.NameAr
            });
        }

        if (slots.Count > 0)
        {
            var typeIds = slots.Select(s => s.CostElementTypeId).ToList();
            var elements = await _costElementRepository.GetAll()
                .Where(e => e.CostElementTypeId.HasValue && typeIds.Contains(e.CostElementTypeId.Value))
                .OrderBy(e => e.Code)
                .ThenBy(e => e.NameEn)
                .Select(e => new { e.Id, TypeId = e.CostElementTypeId.Value, e.Code, e.NameEn, e.NameAr })
                .ToListAsync();

            foreach (var slot in slots)
            {
                slot.Options = elements
                    .Where(e => e.TypeId == slot.CostElementTypeId)
                    .Select(e => new CostElementSlotOptionDto { Id = e.Id, Code = e.Code, NameEn = e.NameEn, NameAr = e.NameAr })
                    .ToList();
            }
        }

        return new ListResultDto<CostElementSlotDto>(slots);
    }
}
