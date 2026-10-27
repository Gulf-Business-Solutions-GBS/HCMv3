using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

[AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollReasonTypes)]
public class PayrollReasonTypesAppService : SimpleLookupAppServiceBase<PayrollReasonType>, IPayrollReasonTypesAppService
{
    public PayrollReasonTypesAppService(IRepository<PayrollReasonType> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_PayrollManagement_PayrollReasonTypes_Create;
    protected override string EditPermissionName => AppPermissions.Pages_PayrollManagement_PayrollReasonTypes_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_PayrollManagement_PayrollReasonTypes_Delete;
}
