using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using Abp.OpenIddict.EntityFrameworkCore.Scopes;
using HCMv3.EntityFrameworkCore;

namespace HCMv3.OpenIddict.Scopes;

public class OpenIddictScopeRepository : EfCoreOpenIddictScopeRepository<HCMv3DbContext>
{
    public OpenIddictScopeRepository(
        IDbContextProvider<HCMv3DbContext> dbContextProvider,
        IUnitOfWorkManager unitOfWorkManager) : base(dbContextProvider, unitOfWorkManager)
    {
    }
}

