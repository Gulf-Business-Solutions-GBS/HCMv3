using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using HCMv3.GridReports.Dto;
using HCMv3.GridReports.Employees;
using HCMv3.OrganizationManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GridReports;

/// <summary>
/// Captions for the OrgLevel1..OrgLevel7 grid-report columns. Port of legacy
/// <c>OrgService.GetOrgUnitTypeColName</c>: the caption of column OrgLevelN is the name of the tenant's
/// <see cref="OrganizationUnitType"/> whose Code is <c>LevelN</c>, NameAr for Arabic, NameEn otherwise.
///
/// One difference from legacy, on purpose: legacy fell back to the raw code ("Level5") for a level the
/// tenant never defined and still showed the (always empty) column. Here such a level is simply left
/// out of the result, and the Angular helper hides its column.
///
/// Only <see cref="AbpAuthorizeAttribute"/> (signed in) is required: the captions are the tenant's own
/// type names and every report screen that needs them has its own page permission.
///
/// Not cached: it is one small query per report screen opening, and that keeps a renamed type visible
/// immediately.
/// </summary>
[AbpAuthorize]
public class GridReportOrgLevelsAppService : HCMv3AppServiceBase, IGridReportOrgLevelsAppService
{
    private readonly IRepository<OrganizationUnitType> _organizationUnitTypeRepository;

    public GridReportOrgLevelsAppService(IRepository<OrganizationUnitType> organizationUnitTypeRepository)
    {
        _organizationUnitTypeRepository = organizationUnitTypeRepository;
    }

    public async Task<ListResultDto<OrgLevelCaptionDto>> GetOrgLevelCaptions()
    {
        var levelCodes = EmployeeOrgLevelNameResolver.LevelCodes;
        var useArabic = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";

        var types = await _organizationUnitTypeRepository.GetAll()
            .Where(t => levelCodes.Contains(t.Code))
            .OrderBy(t => t.Id)
            .Select(t => new { t.Code, t.NameEn, t.NameAr })
            .ToListAsync();

        var result = new List<OrgLevelCaptionDto>();

        for (var i = 0; i < levelCodes.Length; i++)
        {
            // Code is meant to be unique per tenant (OrganizationUnitTypesAppService checks it); take the
            // oldest if bad data ever has two, the same "first match" legacy's FirstOrDefault gave.
            var type = types.FirstOrDefault(t => t.Code == levelCodes[i]);
            if (type == null)
            {
                continue;
            }

            var caption = useArabic ? type.NameAr : type.NameEn;
            if (caption.IsNullOrWhiteSpace())
            {
                caption = useArabic ? type.NameEn : type.NameAr;
            }

            result.Add(new OrgLevelCaptionDto
            {
                Level = i + 1,
                Code = type.Code,
                Caption = caption.IsNullOrWhiteSpace() ? type.Code : caption
            });
        }

        return new ListResultDto<OrgLevelCaptionDto>(result);
    }
}
