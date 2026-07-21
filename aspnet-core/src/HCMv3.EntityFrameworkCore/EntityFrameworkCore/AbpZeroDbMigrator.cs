using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using Abp.MultiTenancy;
using Abp.Zero.EntityFrameworkCore;

namespace HCMv3.EntityFrameworkCore;

public class AbpZeroDbMigrator : AbpZeroDbMigrator<HCMv3DbContext>
{
    public AbpZeroDbMigrator(
        IUnitOfWorkManager unitOfWorkManager,
        IDbPerTenantConnectionStringResolver connectionStringResolver,
        IDbContextResolver dbContextResolver) :
        base(
            unitOfWorkManager,
            connectionStringResolver,
            dbContextResolver)
    {

    }
}

