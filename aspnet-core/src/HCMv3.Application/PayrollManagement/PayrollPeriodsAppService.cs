using Abp.Authorization;
using Abp.Domain.Repositories;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

[AbpAuthorize(AppPermissions.Pages_PayrollManagement_PayrollPeriods)]
public class PayrollPeriodsAppService : SimpleLookupAppServiceBase<PayrollPeriod>, IPayrollPeriodsAppService
{
    public PayrollPeriodsAppService(IRepository<PayrollPeriod> repository) : base(repository)
    {
    }

    protected override string CreatePermissionName => AppPermissions.Pages_PayrollManagement_PayrollPeriods_Create;
    protected override string EditPermissionName => AppPermissions.Pages_PayrollManagement_PayrollPeriods_Edit;
    protected override string DeletePermissionName => AppPermissions.Pages_PayrollManagement_PayrollPeriods_Delete;
}
