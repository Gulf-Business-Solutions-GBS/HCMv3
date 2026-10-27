using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.GovtRelationsManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Legacy <c>OrganizationDocument_Listview_ShowAll</c> (menu "Org Policy", <c>OrgDocumentController</c>): the
/// organisation documents whose <see cref="HCMv3.GeneralMasterInfo.DocumentType.ShowToAll"/> is set, with no
/// create, edit or delete.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_OrgPolicies)]
public class OrgPoliciesAppService : HCMv3AppServiceBase, IOrgPoliciesAppService
{
    private readonly IRepository<OrganizationDocument> _repository;

    public OrgPoliciesAppService(IRepository<OrganizationDocument> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<OrganizationDocumentDto>> GetAll(GetAllOrganizationDocumentsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(d => d.OrganizationUnit)
            .Include(d => d.DocumentType)
            .Include(d => d.Sponsor)
            .Where(d => d.DocumentType.ShowToAll)
            .WhereIf(input.OrganizationUnitId.HasValue, d => d.OrganizationUnitId == input.OrganizationUnitId)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), d =>
                d.DocumentNum.Contains(input.Filter) ||
                d.DocumentType.NameEn.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<OrganizationDocumentDto>(
            totalCount, items.Select(OrganizationDocumentsAppService.ToDto).ToList());
    }
}
