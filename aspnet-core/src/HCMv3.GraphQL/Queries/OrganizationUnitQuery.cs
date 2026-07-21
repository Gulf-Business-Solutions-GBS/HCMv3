using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Organizations;
using GraphQL;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using HCMv3.Authorization;
using HCMv3.Core.Base;
using HCMv3.Core.Extensions;
using HCMv3.Dto;
using HCMv3.Types;

namespace HCMv3.Queries;

public class OrganizationUnitQuery : HCMv3QueryBase<ListGraphType<OrganizationUnitType>, List<OrganizationUnitDto>>
{
    private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;

    public static class Args
    {
        public const string Id = "id";
        public const string TenantId = "tenantId";
        public const string Code = "code";
    }

    public OrganizationUnitQuery(IRepository<OrganizationUnit, long> organizationUnitRepository)
        : base("organizationUnits", new Dictionary<string, Type>
        {
                {Args.Id, typeof(IdGraphType)},
                {Args.TenantId, typeof(IntGraphType)},
                {Args.Code, typeof(StringGraphType)}
        })
    {
        _organizationUnitRepository = organizationUnitRepository;
    }

    [AbpAuthorize(AppPermissions.Pages_Administration_OrganizationUnits)]
    public override async Task<List<OrganizationUnitDto>> Resolve(IResolveFieldContext context)
    {
        var query = _organizationUnitRepository.GetAll().AsNoTracking();

        context
            .ContainsArgument<long>(Args.Id, id => query = query.Where(o => o.Id == id))
            .ContainsArgument<int?>(Args.TenantId, tenantId => query = query.Where(o => o.TenantId == tenantId.Value))
            .ContainsArgument<string>(Args.Code, code => query = query.Where(o => o.Code == code));

        var organizationUnits = await query.ToListAsync();
        return ObjectMapper.Map<List<OrganizationUnitDto>>(organizationUnits);
    }
}

