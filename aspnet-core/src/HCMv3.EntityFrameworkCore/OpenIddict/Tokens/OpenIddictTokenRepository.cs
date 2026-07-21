using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using Abp.OpenIddict.EntityFrameworkCore.Tokens;
using HCMv3.EntityFrameworkCore;

namespace HCMv3.OpenIddict.Tokens;

public class OpenIddictTokenRepository : EfCoreOpenIddictTokenRepository<HCMv3DbContext>
{
    public OpenIddictTokenRepository(
        IDbContextProvider<HCMv3DbContext> dbContextProvider,
        IUnitOfWorkManager unitOfWorkManager) : base(dbContextProvider, unitOfWorkManager)
    {
    }
}

