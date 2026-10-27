using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

[AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketRoutes)]
public class TicketRoutesAppService : SimpleLookupAppServiceBase<TicketRoute>, ITicketRoutesAppService
{
    public TicketRoutesAppService(IRepository<TicketRoute> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_PayrollManagement_TicketRoutes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_PayrollManagement_TicketRoutes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_PayrollManagement_TicketRoutes_Delete;
}
