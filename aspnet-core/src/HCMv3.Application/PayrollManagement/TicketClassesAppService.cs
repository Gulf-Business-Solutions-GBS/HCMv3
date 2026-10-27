using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

[AbpAuthorize(AppPermissions.Pages_PayrollManagement_TicketClasses)]
public class TicketClassesAppService : SimpleLookupAppServiceBase<TicketClass>, ITicketClassesAppService
{
    public TicketClassesAppService(IRepository<TicketClass> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_PayrollManagement_TicketClasses_Create;
    protected override string EditPermissionName => AppPermissions.Pages_PayrollManagement_TicketClasses_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_PayrollManagement_TicketClasses_Delete;
}
